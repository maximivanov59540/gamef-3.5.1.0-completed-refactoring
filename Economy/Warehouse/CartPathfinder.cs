using System.Collections.Generic;
using UnityEngine;

public class CartPathfinder : MonoBehaviour
{
    private GridSystem _gridSystem;
    private IRoadManager _roadManager;

    public void Initialize()
    {
        _gridSystem = Object.FindFirstObjectByType<GridSystem>();
        _roadManager = ServiceLocator.Get<IRoadManager>();
    }

    public bool TryFindPath(Vector2Int currentPos, Vector2Int targetPos, out List<Vector2Int> path)
    {
        path = null;
        if (_gridSystem == null || _roadManager == null) return false;

        var graph = _roadManager.GetRoadGraph();
        if (graph == null) return false;

        // 1. Входы на дорогу
        var startNodes = LogisticsPathfinder.FindAllRoadAccess(currentPos, _gridSystem, graph);
        var endNodes = LogisticsPathfinder.FindAllRoadAccess(targetPos, _gridSystem, graph);

        if (startNodes.Count == 0 || endNodes.Count == 0) return false;

        // 2. Поиск пути (упрощенно берем первую пару)
        // В идеале тут нужен поиск кратчайшего пути между двумя МНОЖЕСТВАМИ точек
        foreach(var start in startNodes)
        {
            // BFS flood fill для дистанций
            var distMap = LogisticsPathfinder.Distances_BFS(start, 500, graph);
            
            Vector2Int bestEnd = new Vector2Int(-1, -1);
            int minD = int.MaxValue;

            foreach(var end in endNodes)
            {
                if (distMap.TryGetValue(end, out int d) && d < minD)
                {
                    minD = d;
                    bestEnd = end;
                }
            }

            if (bestEnd.x != -1)
            {
                path = LogisticsPathfinder.FindActualPath(start, bestEnd, graph);
                // Добавляем старт и финиш (от здания до дороги)
                path.Insert(0, currentPos);
                path.Add(targetPos);
                return true;
            }
        }

        return false;
    }
}