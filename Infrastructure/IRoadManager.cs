using System.Collections.Generic;
using UnityEngine;

public interface IRoadManager : IGameService
{
    void PlaceRoad(Vector2Int gridPos, RoadData data);
    void RemoveRoad(Vector2Int gridPos);
    void UpgradeRoad(Vector2Int gridPos, RoadData newData);
    
    // Доступ к графу дорог
    Dictionary<Vector2Int, HashSet<Vector2Int>> GetRoadGraph();
    
    // Логистика
    void CreateRequest(ResourceRequest request);
    void FulfillRequest(ResourceRequest request);
    ResourceRequest GetBestRequest(Vector2Int cartGridPos, ResourceType resourceToDeliver, float roadRadius);
    
    // События
    event System.Action<Vector2Int> OnRoadAdded;
    event System.Action<Vector2Int> OnRoadRemoved;
}