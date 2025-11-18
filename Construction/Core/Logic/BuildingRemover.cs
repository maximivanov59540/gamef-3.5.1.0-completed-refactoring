using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отвечает ТОЛЬКО за удаление зданий и возврат ресурсов (Refund).
/// </summary>
public class BuildingRemover : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Сколько ресурсов возвращается при сносе (0.5 = 50%)")]
    [SerializeField] private float _refundPercentage = 0.5f;

    private GridSystem _gridSystem;
    private RoadManager _roadManager;
    private ResourceManager _resourceManager; // В будущем заменим на IResourceManager через ServiceLocator

    public void Initialize()
    {
        _gridSystem = Object.FindFirstObjectByType<GridSystem>();
        _roadManager = RoadManager.Instance;
        _resourceManager = ResourceManager.Instance;
    }

    /// <summary>
    /// Удаляет здание по координатам.
    /// </summary>
    public void DeleteBuildingAt(Vector2Int gridPos)
    {
        // 1. Пробуем удалить здание
        BuildingIdentity id = _gridSystem.GetBuildingIdentityAt(gridPos.x, gridPos.y);
        if (id != null)
        {
            DeleteBuilding(id);
            return;
        }

        // 2. Пробуем удалить дорогу (если здания нет)
        RoadTile road = _gridSystem.GetRoadTileAt(gridPos.x, gridPos.y);
        if (road != null)
        {
            _roadManager.RemoveRoad(gridPos);
            // Можно добавить возврат ресурсов за дорогу, если нужно
        }
    }

    /// <summary>
    /// Удаляет конкретное здание и возвращает ресурсы.
    /// </summary>
    public void DeleteBuilding(BuildingIdentity identity)
    {
        if (identity == null) return;

        // 1. Возврат ресурсов (если это не чертеж)
        if (!identity.isBlueprint)
        {
            ReturnResources(identity.buildingData);
        }

        // 2. Снятие с сетки (GridSystem сама удалит GO и почистит ссылки)
        _gridSystem.ClearCell(identity.rootGridPosition.x, identity.rootGridPosition.y);
        
        Debug.Log($"[BuildingRemover] Здание {identity.name} удалено.");
    }

    private void ReturnResources(BuildingData data)
    {
        if (data == null || data.costs == null) return;

        foreach (var cost in data.costs)
        {
            int refundAmount = Mathf.FloorToInt(cost.amount * _refundPercentage);
            if (refundAmount > 0)
            {
                _resourceManager.AddToStorage(cost.resourceType, refundAmount);
            }
        }
        
        // TODO: Показать всплывающий текст с возвращенными ресурсами через NotificationManager
    }
}