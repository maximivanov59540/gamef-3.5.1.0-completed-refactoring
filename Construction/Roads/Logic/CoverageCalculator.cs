using System.Collections.Generic;
using UnityEngine;

public class CoverageCalculator
{
    private readonly GridSystem _grid;
    private readonly IRoadManager _roadManager;

    public CoverageCalculator()
    {
        _grid = Object.FindFirstObjectByType<GridSystem>();
        _roadManager = ServiceLocator.Get<IRoadManager>();
    }

    // Рассчитывает карту эффективности для всех источников
    public Dictionary<RoadTile, float> CalculateCoverage(List<AuraEmitter> sources)
    {
        var mergedEff = new Dictionary<RoadTile, float>();
        var graph = _roadManager.GetRoadGraph();
        if (graph == null) return mergedEff;

        foreach (var src in sources)
        {
            float radius = src.radius;
            Vector2Int root = src.GetRootPosition();
            int maxSteps = Mathf.FloorToInt(radius / Mathf.Max(0.01f, _grid.GetCellSize()));

            // 1. Находим точки входа (BFS Seeds)
            var seeds = LogisticsPathfinder.FindAllRoadAccess(root, _grid, graph);
            if (seeds.Count == 0) continue;

            // 2. Считаем дистанции
            var dists = LogisticsPathfinder.Distances_BFS_Multi(seeds, maxSteps, graph);

            // 3. Конвертируем в эффективность
            foreach (var kvp in dists)
            {
                float eff = EvaluateEfficiency(kvp.Value, maxSteps);
                RoadTile tile = _grid.GetRoadTileAt(kvp.Key.x, kvp.Key.y);
                
                if (tile != null)
                {
                    if (!mergedEff.TryGetValue(tile, out float current) || eff > current)
                        mergedEff[tile] = eff;
                }
            }
        }
        return mergedEff;
    }

    private float EvaluateEfficiency(int steps, int maxSteps)
    {
        if (maxSteps <= 0) return 1f;
        if (steps <= 0.9f * maxSteps) return 1f;
        
        float t = Mathf.InverseLerp(0.9f * maxSteps, maxSteps, steps);
        return 1f - (t * t);
    }
}