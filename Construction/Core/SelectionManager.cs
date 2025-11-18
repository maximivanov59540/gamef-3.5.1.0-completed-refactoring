using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Менеджер выделения зданий.
/// Отвечает за логику выделения (клик, рамка) и визуализацию (круг, оверлей дорог).
/// REFACTORED: Использует ServiceLocator для IAuraManager.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GridSystem _gridSystem;
    [SerializeField] private PlayerInputController _playerInputController;
    [SerializeField] private LineRenderer _selectionBoxRenderer; // Рамка выделения

    // Зависимость через интерфейс
    private IAuraManager _auraManager;

    private HashSet<BuildingIdentity> _selectedBuildings = new HashSet<BuildingIdentity>();
    private Vector3 _startWorldPos; // Точка начала выделения рамкой

    // Событие изменения выделения
    public event System.Action<IReadOnlyCollection<BuildingIdentity>> SelectionChanged;
    private void RaiseSelectionChanged() => SelectionChanged?.Invoke(_selectedBuildings);

    private void Awake()
    {
        // Выключаем рамку при старте
        if (_selectionBoxRenderer != null)
        {
            _selectionBoxRenderer.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("SelectionManager: Не назначен '_selectionBoxRenderer'. Выделение рамкой не будет видно.");
        }

        // Авто-поиск ссылок, если не назначены
        if (_gridSystem == null) _gridSystem = FindFirstObjectByType<GridSystem>();
        if (_playerInputController == null) _playerInputController = FindFirstObjectByType<PlayerInputController>();
    }

    private void Start()
    {
        // Получаем AuraManager через Service Locator
        _auraManager = ServiceLocator.Get<IAuraManager>();
    }

    /// <summary>
    /// Полностью очищает текущее выделение.
    /// </summary>
    public void ClearSelection()
    {
        _selectedBuildings.Clear();

        // Скрываем оверлей дорог через интерфейс
        _auraManager?.HideRoadAuraOverlay();
        
        RaiseSelectionChanged();
    }

    // --- РАБОТА С РАМКОЙ (BOX SELECTION) ---

    public void StartSelection(Vector3 worldPos)
    {
        // 1. Очищаем старое выделение
        ClearSelection();

        // 2. Запоминаем начало
        _startWorldPos = worldPos;

        // 3. Включаем рамку
        if (_selectionBoxRenderer != null)
        {
            _selectionBoxRenderer.gameObject.SetActive(true);
        }
    }

    public void UpdateSelection(Vector3 currentWorldPos)
    {
        if (_selectionBoxRenderer == null) return;

        // Рисуем прямоугольник на земле (Y чуть выше 0)
        float y = 0.1f;
        _startWorldPos.y = y;
        currentWorldPos.y = y;

        // 4 точки прямоугольника
        Vector3 p1 = _startWorldPos;
        Vector3 p2 = new Vector3(currentWorldPos.x, y, _startWorldPos.z);
        Vector3 p3 = currentWorldPos;
        Vector3 p4 = new Vector3(_startWorldPos.x, y, currentWorldPos.z);

        _selectionBoxRenderer.SetPosition(0, p1);
        _selectionBoxRenderer.SetPosition(1, p2);
        _selectionBoxRenderer.SetPosition(2, p3);
        _selectionBoxRenderer.SetPosition(3, p4);
        _selectionBoxRenderer.SetPosition(4, p1); // Замыкаем
    }

    public void HideSelectionVisuals()
    {
        if (_selectionBoxRenderer != null)
        {
            _selectionBoxRenderer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Завершает выделение рамкой: находит здания внутри и выделяет их.
    /// </summary>
    public HashSet<BuildingIdentity> FinishSelectionAndSelect(Vector3 endWorldPos)
    {
        HideSelectionVisuals();

        // Переводим мировые координаты в координаты сетки
        _gridSystem.GetXZ(_startWorldPos, out int startX, out int startZ);
        _gridSystem.GetXZ(endWorldPos, out int endX, out int endZ);

        Vector2Int startGridPos = new Vector2Int(startX, startZ);
        Vector2Int endGridPos = new Vector2Int(endX, endZ);

        // Собираем здания
        HashSet<BuildingIdentity> found = _gridSystem.GetBuildingsInRect(startGridPos, endGridPos);

        _selectedBuildings = found;
        RaiseSelectionChanged();

        // Показываем зоны влияния для выбранных зданий (если есть)
        ShowRoadAurasForSelection();

        return _selectedBuildings;
    }

    // --- РАБОТА С ОДИНОЧНЫМ ВЫДЕЛЕНИЕМ ---

    public void SelectSingle(BuildingIdentity building)
    {
        ClearSelection();
        
        if (building != null)
        {
            _selectedBuildings.Add(building);
        }
        
        ShowRoadAurasForSelection();
        RaiseSelectionChanged();
    }

    // --- ВИЗУАЛИЗАЦИЯ РАДИУСОВ И АУР ---

    /// <summary>
    /// Показывает радиус (круг) и дорожную ауру для конкретного здания.
    /// Вызывается при наведении или клике.
    /// </summary>
    public void ShowRadius(BuildingIdentity building)
    {
        if (building == null) return;

        // 1. Круглый радиус (старый визуализатор на объекте)
        RadiusVisualizer visualizer = building.GetComponentInChildren<RadiusVisualizer>();
        if (visualizer != null)
        {
            visualizer.Show();
        }

        // 2. Дорожная аура (через AuraManager)
        var emitter = building.GetComponent<AuraEmitter>();
        if (emitter != null && emitter.distributionType == AuraDistributionType.RoadBased)
        {
            _auraManager?.ShowRoadAura(emitter);
        }
    }

    /// <summary>
    /// Скрывает радиус и дорожную ауру.
    /// </summary>
    public void HideRadius(BuildingIdentity building)
    {
        if (building == null) return;

        // 1. Скрываем круг
        RadiusVisualizer visualizer = building.GetComponentInChildren<RadiusVisualizer>();
        if (visualizer != null)
        {
            visualizer.Hide();
        }

        // 2. Скрываем оверлей дорог (если это было RoadBased здание)
        var emitter = building.GetComponent<AuraEmitter>();
        if (emitter != null && emitter.distributionType == AuraDistributionType.RoadBased)
        {
            _auraManager?.HideRoadAuraOverlay();
        }
    }

    /// <summary>
    /// Показывает дорожные ауры для всех выбранных зданий.
    /// (Использует IAuraManager)
    /// </summary>
    private void ShowRoadAurasForSelection()
    {
        if (_auraManager == null) return;

        // Сначала сбросим старый оверлей
        _auraManager.HideRoadAuraOverlay();

        if (_selectedBuildings == null || _selectedBuildings.Count == 0) return;

        // Проходим по всем выбранным зданиям
        foreach (var b in _selectedBuildings)
        {
            if (b == null) continue;
            
            var emitter = b.GetComponent<AuraEmitter>();
            
            // Если у здания есть дорожная аура — просим AuraManager её показать.
            // (RoadCoverageVisualizer умеет объединять несколько источников)
            if (emitter != null && emitter.distributionType == AuraDistributionType.RoadBased)
            {
                _auraManager.ShowRoadAura(emitter);
            }
        }
    }

    /// <summary>
    /// Обновляет ауру для текущего единственного выделения (если нужно принудительно обновить).
    /// </summary>
    public void UpdateAuraForCurrentSelection()
    {
        if (_auraManager == null) return;

        if (_selectedBuildings != null && _selectedBuildings.Count == 1)
        {
            var b = _selectedBuildings.First();
            if (b != null)
            {
                var emitter = b.GetComponent<AuraEmitter>();
                if (emitter != null && emitter.distributionType == AuraDistributionType.RoadBased)
                {
                    _auraManager.ShowRoadAura(emitter);
                    return;
                }
            }
        }

        _auraManager.HideRoadAuraOverlay();
    }
}