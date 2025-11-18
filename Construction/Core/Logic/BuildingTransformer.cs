using UnityEngine;

/// <summary>
/// Отвечает за трансформации: Перемещение, Поворот, Копирование, Апгрейд.
/// </summary>
public class BuildingTransformer : MonoBehaviour
{
    private GridSystem _gridSystem;
    private BuildingPlacer _placer; // Нужен для создания копий
    private BuildingValidator _validator;

    // Состояние перемещения
    public bool IsHoldingBuilding { get; private set; } = false;
    private GameObject _liftedBuildingObject;
    private BuildingIdentity _liftedIdentity;
    private Vector2Int _originalPos;
    private float _originalRot;

    public void Initialize(BuildingPlacer placer, BuildingValidator validator)
    {
        _gridSystem = Object.FindFirstObjectByType<GridSystem>();
        _placer = placer;
        _validator = validator;
    }

    // --- ПЕРЕМЕЩЕНИЕ ---

    public void PickUpBuilding(Vector2Int gridPos)
    {
        if (IsHoldingBuilding) return;

        // Используем GridSystem для "изъятия" здания из сетки
        GameObject lifted = _gridSystem.PickUpBuilding(gridPos.x, gridPos.y);
        
        if (lifted != null)
        {
            _liftedBuildingObject = lifted;
            _liftedIdentity = lifted.GetComponent<BuildingIdentity>();
            _originalPos = _liftedIdentity.rootGridPosition;
            _originalRot = _liftedIdentity.yRotation;
            
            IsHoldingBuilding = true;

            // Визуально превращаем в призрака
            var visuals = lifted.GetComponent<BuildingVisuals>();
            if (visuals) visuals.SetState(VisualState.Ghost, true);

            // Отключаем производство на время переноса
            var producer = lifted.GetComponent<ResourceProducer>();
            if (producer) producer.IsPaused = true; // Или disable component
        }
    }

    public bool TryPlaceLiftedBuilding(Vector2Int newPos, float rotation)
    {
        if (!IsHoldingBuilding || _liftedBuildingObject == null) return false;

        BuildingData data = _liftedIdentity.buildingData;
        
        // Валидация: проверяем место (ресурсы не нужны для перемещения)
        // Пересчитываем размер с учетом поворота
        Vector2Int size = data.size;
        if (Mathf.Abs(rotation - 90f) < 1f || Mathf.Abs(rotation - 270f) < 1f)
            size = new Vector2Int(data.size.y, data.size.x);

        if (!_validator.IsAreaClear(newPos, size) || !_validator.CheckGhostCollision(_liftedBuildingObject))
        {
            return false;
        }

        // Размещение
        _liftedIdentity.rootGridPosition = newPos;
        _liftedIdentity.yRotation = rotation;

        // Позиционирование GO
        Vector3 worldPos = _gridSystem.GetWorldPosition(newPos.x, newPos.y);
        float cellSize = _gridSystem.GetCellSize();
        worldPos.x += (size.x * cellSize) / 2f;
        worldPos.z += (size.y * cellSize) / 2f;
        
        _liftedBuildingObject.transform.position = worldPos;
        _liftedBuildingObject.transform.rotation = Quaternion.Euler(0, rotation, 0);

        // Регистрация в сетке
        _gridSystem.OccupyCells(_liftedIdentity, size);

        // Возврат визуала
        var visuals = _liftedBuildingObject.GetComponent<BuildingVisuals>();
        if (visuals) visuals.SetState(_liftedIdentity.isBlueprint ? VisualState.Blueprint : VisualState.Real, true);

        // Включаем производство
        var producer = _liftedBuildingObject.GetComponent<ResourceProducer>();
        if (producer) producer.IsPaused = false;

        IsHoldingBuilding = false;
        _liftedBuildingObject = null;
        _liftedIdentity = null;
        
        return true;
    }

    public void CancelMove()
    {
        if (!IsHoldingBuilding) return;
        // Возвращаем на старое место
        TryPlaceLiftedBuilding(_originalPos, _originalRot);
    }

    // --- КОПИРОВАНИЕ ---
    
    public void CopyBuilding(Vector2Int sourcePos, Vector2Int targetPos, bool blueprintMode)
    {
        var sourceId = _gridSystem.GetBuildingIdentityAt(sourcePos.x, sourcePos.y);
        if (sourceId == null) return;

        // Делегируем создание BuildingPlacer'у, но здесь можно добавить логику копирования настроек
        // (например, скопировать настройки рецепта ResourceProducer'а)
        
        // Сейчас просто вызываем создание нового через менеджер (внешний вызов)
        // Эта логика чаще находится в State_Copying, который вызывает BuildingManager.EnterBuildMode
    }
}