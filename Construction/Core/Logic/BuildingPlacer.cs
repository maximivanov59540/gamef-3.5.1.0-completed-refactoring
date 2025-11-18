using UnityEngine;

/// <summary>
/// Отвечает ТОЛЬКО за создание объектов (призраков и реальных зданий) и их визуал.
/// </summary>
public class BuildingPlacer : MonoBehaviour
{
    private GridSystem _gridSystem;
    
    public void Initialize()
    {
        _gridSystem = Object.FindFirstObjectByType<GridSystem>();
    }

    public GameObject CreateGhost(BuildingData data)
    {
        if (data == null || data.buildingPrefab == null) return null;
        
        var ghost = Instantiate(data.buildingPrefab);
        ghost.layer = LayerMask.NameToLayer("Ghost");
        
        // Отключаем логику на призраке
        var prod = ghost.GetComponent<ResourceProducer>();
        if (prod) prod.enabled = false;
        
        // Добавляем коллайдер для проверок
        if (!ghost.GetComponent<GhostBuildingCollider>())
            ghost.AddComponent<GhostBuildingCollider>();
            
        return ghost;
    }

    public GameObject PlaceBuilding(BuildingData data, Vector2Int gridPos, float rotation, bool isBlueprint)
    {
        if (_gridSystem == null) return null;

        Vector2Int size = data.size;
        // Учет поворота
        if (Mathf.Abs(rotation - 90f) < 1f || Mathf.Abs(rotation - 270f) < 1f)
            size = new Vector2Int(data.size.y, data.size.x);

        // Позиция
        Vector3 worldPos = _gridSystem.GetWorldPosition(gridPos.x, gridPos.y);
        float cellSize = _gridSystem.GetCellSize();
        worldPos.x += (size.x * cellSize) / 2f;
        worldPos.z += (size.y * cellSize) / 2f;

        // Инстанцирование
        GameObject building = Instantiate(data.buildingPrefab, worldPos, Quaternion.Euler(0, rotation, 0));
        building.layer = LayerMask.NameToLayer("Buildings");

        // Инициализация Identity
        var identity = building.GetComponent<BuildingIdentity>();
        if (!identity) identity = building.AddComponent<BuildingIdentity>();
        
        identity.buildingData = data;
        identity.rootGridPosition = gridPos;
        identity.yRotation = rotation;
        identity.isBlueprint = isBlueprint;

        // Регистрация в сетке
        _gridSystem.OccupyCells(identity, size);

        // Визуал
        var visuals = building.GetComponent<BuildingVisuals>();
        if (visuals) 
            visuals.SetState(isBlueprint ? VisualState.Blueprint : VisualState.Real, true);

        return building;
    }
}