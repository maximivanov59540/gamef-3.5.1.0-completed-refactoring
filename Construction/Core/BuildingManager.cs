using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    // --- Components ---
    private BuildingValidator _validator;
    private BuildingPlacer _placer;
    
    // --- State ---
    private BuildingData _currentData;
    private GameObject _currentGhost;
    private float _currentRotation;
    
    // Флаг для старой логики (пока мы не отрефакторили GroupHandler)
    public bool CanPlace { get; private set; } 

    [Header("References")]
    public GameObject gridVisual;
    private NotificationManager _notifications;
    private PlayerInputController _input;
    private AuraManager _auraManager;

    // Blueprint Mode State
    public bool IsBlueprintModeActive { get; private set; } = false;

    // Статическое свойство для совместимости с другими скриптами
    public bool IsBlueprintMode => IsBlueprintModeActive;

    void Awake()
    {
        // Init sub-components
        _validator = gameObject.AddComponent<BuildingValidator>();
        _placer = gameObject.AddComponent<BuildingPlacer>();
        
        _notifications = FindFirstObjectByType<NotificationManager>();
        _input = FindFirstObjectByType<PlayerInputController>();
        _auraManager = AuraManager.Instance;
    }

    void Start()
    {
        _validator.Initialize();
        _placer.Initialize();
        
        // Подписка на событие долга (как было в оригинале)
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnDebtStatusChanged += HandleDebtStatusChanged;
        }
    }
    
    void OnDestroy()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnDebtStatusChanged -= HandleDebtStatusChanged;
        }
    }

    private void HandleDebtStatusChanged(bool isInDebt)
    {
        // Логика реакции на долг (если нужна)
    }

    // --- PUBLIC API (Фасад) ---

    public void EnterBuildMode(BuildingData data)
    {
        CancelAllModes();
        _currentData = data;
        _currentRotation = 0f;
        
        _currentGhost = _placer.CreateGhost(data);
        // Первичная настройка визуала
        SetBuildingVisuals(_currentGhost, VisualState.Ghost, true);
        
        ShowGrid(true);
        _input.SetMode(InputMode.Building);
    }

    public void UpdateGhostPosition(Vector2Int gridPos, Vector3 worldPos)
    {
        if (_currentGhost == null) return;

        // 1. Позиционирование
        float cellSize = FindFirstObjectByType<GridSystem>().GetCellSize(); // Или кэшировать GridSystem
        Vector2Int size = GetRotatedSize();
        
        // Центрирование по сетке (важно для визуального совпадения)
        Vector3 finalPos = worldPos;
        finalPos.x = gridPos.x * cellSize + (size.x * cellSize) / 2f;
        finalPos.z = gridPos.y * cellSize + (size.y * cellSize) / 2f;
        finalPos.y = worldPos.y; // Сохраняем высоту (земли)

        _currentGhost.transform.position = finalPos;
        _currentGhost.transform.rotation = Quaternion.Euler(0, _currentRotation, 0);

        // 2. Валидация через Validator
        CheckPlacementValidity(_currentGhost, _currentData, gridPos);
    }

    public void TryPlaceBuilding(Vector2Int gridPos)
    {
        if (_currentData == null) return;

        // 1. Проверяем валидность (используем сохраненный флаг CanPlace или пересчитываем)
        if (!CanPlace)
        {
            _notifications?.ShowNotification("Место занято или недопустимо!");
            return;
        }

        // 2. Оплата (если не чертеж)
        if (!IsBlueprintModeActive)
        {
            if (!_validator.CanAfford(_currentData))
            {
                _notifications?.ShowNotification("Недостаточно ресурсов!");
                return;
            }
            ResourceManager.Instance.SpendResources(_currentData);
            MoneyManager.Instance.SpendMoney(_currentData.moneyCost);
        }

        // 3. Постройка через Placer
        GameObject building = _placer.PlaceBuilding(_currentData, gridPos, _currentRotation, IsBlueprintModeActive);
        
        // 4. Эффекты
        if (building != null)
        {
             _notifications?.ShowNotification(IsBlueprintModeActive ? "Проект размещен!" : "Построено!");
        }
    }
    
    public void RotateGhost()
    {
        _currentRotation = (_currentRotation + 90f) % 360f;
    }

    public void CancelAllModes()
    {
        if (_currentGhost) Destroy(_currentGhost);
        _currentGhost = null;
        _currentData = null;
        ShowGrid(false);
        
        // Прячем превью ауры
        if (_auraManager != null) _auraManager.HideRoadAuraPreview();
    }

    public void ShowGrid(bool show)
    {
        if (gridVisual) gridVisual.SetActive(show);
    }
    
    // ========================================================================
    //   МЕТОДЫ СОВМЕСТИМОСТИ (FIX CS1061 ERRORS)
    //   Эти методы нужны для GroupOperationHandler и State_Moving
    // ========================================================================

    /// <summary>
    /// Используется в GroupOperationHandler для массовой вставки
    /// </summary>
    public bool PlaceBuildingFromOrder(BuildingData data, Vector2Int gridPos, float rotation, bool isBlueprint)
    {
        // 1. Расчет размера
        Vector2Int size = data.size;
        if (Mathf.Abs(rotation - 90f) < 1f || Mathf.Abs(rotation - 270f) < 1f)
            size = new Vector2Int(data.size.y, data.size.x);

        // 2. Проверка места
        if (!_validator.IsAreaClear(gridPos, size)) return false;

        // 3. Проверка ресурсов (если не чертеж)
        if (!isBlueprint)
        {
            if (!_validator.CanAfford(data)) return false;
            
            // Списываем ресурсы
            ResourceManager.Instance.SpendResources(data);
            MoneyManager.Instance.SpendMoney(data.moneyCost);
        }

        // 4. Строим
        _placer.PlaceBuilding(data, gridPos, rotation, isBlueprint);
        return true;
    }

    /// <summary>
    /// Используется везде для смены цвета призрака (зеленый/красный/синий)
    /// </summary>
    public void SetBuildingVisuals(GameObject building, VisualState state, bool isValid)
    {
        if (building == null) return;
        var visuals = building.GetComponent<BuildingVisuals>();
        if (visuals != null)
        {
            visuals.SetState(state, isValid);
        }
    }

    /// <summary>
    /// Используется в GroupOperationHandler для проверки призраков
    /// </summary>
    public void CheckPlacementValidity(GameObject objectToCheck, BuildingData data, Vector2Int rootPos)
    {
        if (objectToCheck == null || data == null) 
        {
            CanPlace = false;
            return;
        }

        // 1. Считаем размер с учетом поворота объекта
        float rotation = objectToCheck.transform.eulerAngles.y;
        Vector2Int size = data.size;
        if (Mathf.Abs(rotation - 90f) < 1f || Mathf.Abs(rotation - 270f) < 1f)
            size = new Vector2Int(data.size.y, data.size.x);

        // 2. Проверяем логику (сетка) и физику (коллайдеры)
        bool isGridClear = _validator.IsAreaClear(rootPos, size);
        bool isPhysicsClear = _validator.CheckGhostCollision(objectToCheck);
        
        CanPlace = isGridClear && isPhysicsClear;

        // 3. Обновляем визуал
        VisualState state = IsBlueprintModeActive ? VisualState.Blueprint : VisualState.Ghost;
        SetBuildingVisuals(objectToCheck, state, CanPlace);
    }
    
    // Методы для удаления и перемещения (заглушки или делегаты к старой логике, если нужно)
    public void TryDeleteBuilding(Vector2Int gridPos)
    {
        // Пока используем старую логику через GridSystem, но можно перенести в BuildingRemover
        var id = FindFirstObjectByType<GridSystem>().GetBuildingIdentityAt(gridPos.x, gridPos.y);
        if (id != null)
        {
            DeleteBuilding(id);
        }
    }
    
    public void DeleteBuilding(BuildingIdentity identity)
    {
        // Простая реализация удаления (можно расширить в BuildingRemover)
        if (identity == null) return;
        
        // Возврат ресурсов (50%)
        if (!identity.isBlueprint && identity.buildingData.costs != null)
        {
            foreach (var cost in identity.buildingData.costs)
            {
                ResourceManager.Instance.AddToStorage(cost.resourceType, Mathf.Floor(cost.amount * 0.5f));
            }
            _notifications?.ShowNotification("Вернулось 50% ресурсов");
        }
        
        // Очистка сетки
        FindFirstObjectByType<GridSystem>().ClearCell(identity.rootGridPosition.x, identity.rootGridPosition.y);
    }
    
    public bool IsHoldingBuilding()
    {
        // Для упрощения пока вернем false, так как логика Move перенесена
        // Если нужно полноценное перемещение, нужно добавить BuildingTransformer
        return false; 
    }

    public void TryPickUpBuilding(Vector2Int gridPos)
    {
        // Заглушка. Реальная логика должна быть в BuildingTransformer
        _notifications?.ShowNotification("Перемещение временно недоступно (WIP)");
    }

    // ========================================================================
    //   HELPERS
    // ========================================================================

    private Vector2Int GetRotatedSize()
    {
        if (_currentData == null) return Vector2Int.one;
        if (Mathf.Abs(_currentRotation - 90f) < 1f || Mathf.Abs(_currentRotation - 270f) < 1f)
            return new Vector2Int(_currentData.size.y, _currentData.size.x);
        return _currentData.size;
    }
    public bool TryPlaceBuilding_MassBuild(Vector2Int gridPos)
    {
        // Защита
        if (_currentData == null) return false;

        // 1. Проверка ресурсов (если мы строим не чертеж)
        if (!IsBlueprintModeActive)
        {
            // Используем новый компонент _validator для проверки
            if (!_validator.CanAfford(_currentData))
            {
                _notifications?.ShowNotification("Недостаточно ресурсов!");
                return false; // Возвращаем false, чтобы остановить массовую постройку
            }

            // Списываем ресурсы и деньги
            ResourceManager.Instance.SpendResources(_currentData);
            MoneyManager.Instance.SpendMoney(_currentData.moneyCost);
        }

        // 2. Строим здание через новый компонент _placer
        _placer.PlaceBuilding(_currentData, gridPos, _currentRotation, IsBlueprintModeActive);

        return true; // Успех
    }
    
    // Хелперы для State Machines
    public BuildingData GetCurrentGhostData() => _currentData;
    public float GetCurrentGhostRotation() => _currentRotation;
    public AuraEmitter GetGhostAuraEmitter() => _currentGhost ? _currentGhost.GetComponent<AuraEmitter>() : null;
    public void ShowGhost(bool show) { if (_currentGhost) _currentGhost.SetActive(show); }
    
    // Методы для апгрейдов (заглушки, чтобы не ломать компиляцию)
    public void TryUpgradeBuilding(Vector2Int gridPos) { /* TODO: Implement in BuildingTransformer */ }
    public void MassUpgrade(HashSet<BuildingIdentity> buildings) { /* TODO */ }
    public void TryCopyBuilding(Vector2Int gridPos) { /* TODO */ }
}