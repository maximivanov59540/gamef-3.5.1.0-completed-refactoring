using UnityEngine;
using System.Collections.Generic;

public class AuraManager : MonoBehaviour, IAuraManager
{
    public static AuraManager Instance { get; private set; } // Оставляем пока для совместимости

    [SerializeField] private RoadCoverageVisualizer _coverage;
    
    // Зависимости через интерфейсы
    private GridSystem _gridSystem;
    private IRoadManager _roadManager;

    private readonly HashSet<AuraEmitter> _allEmitters = new HashSet<AuraEmitter>();
    private readonly Vector2Int[] _neighborOffsets = 
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 0), new Vector2Int(-1, 0)
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Получаем зависимости
        _gridSystem = FindFirstObjectByType<GridSystem>();
        _roadManager = ServiceLocator.Get<IRoadManager>();
        
        if (_coverage == null) 
            _coverage = FindFirstObjectByType<RoadCoverageVisualizer>();
    }

    // --- Реализация IAuraManager ---

    public void RegisterEmitter(AuraEmitter emitter) => _allEmitters.Add(emitter);
    public void UnregisterEmitter(AuraEmitter emitter) => _allEmitters.Remove(emitter);

    public bool IsPositionInAura(Vector3 worldPos, AuraType type)
    {
        foreach (AuraEmitter emitter in _allEmitters)
        {
            if (emitter == null || emitter.type != type) continue;

            if (emitter.distributionType == AuraDistributionType.Radial)
            {
                if (Vector3.Distance(worldPos, emitter.transform.position) <= emitter.radius) return true;
            }
            else if (emitter.distributionType == AuraDistributionType.RoadBased)
            {
                if (_gridSystem == null || _roadManager == null) continue;

                BuildingIdentity emitterId = emitter.GetIdentity();
                if (emitterId == null) continue;

                Vector2Int start = GetRoadAccessCell(emitterId);
                if (start.x == -1) continue;

                _gridSystem.GetXZ(worldPos, out int gx, out int gz);
                Vector2Int end = GetRoadAccessCellForPos(new Vector2Int(gx, gz));
                if (end.x == -1) continue;

                if (start == end) return true;
                
                // Проверка пути через граф (используем IRoadManager)
                var graph = _roadManager.GetRoadGraph();
                if (LogisticsPathfinder.HasPath_BFS(start, end, graph)) return true;
            }
        }
        return false;
    }

    public void ShowRoadAura(AuraEmitter emitter)
    {
        if (emitter == null || _coverage == null) return;
        emitter.RefreshRootCell();
        _coverage.ShowForEmitter(emitter);
    }

    public void ShowRoadAuraPreview(Vector3 worldPos, Vector2Int gridPos, Vector2Int baseSize, float rotation, float radius)
    {
        if (_coverage == null) return;
        if (CheckIfTouchingRoad(gridPos, baseSize, rotation))
        {
            _coverage.ShowPreview(worldPos, radius);
        }
        else
        {
            _coverage.HidePreview();
        }
    }

    public void HideRoadAuraOverlay() => _coverage?.HideAll();
    public void HideRoadAuraPreview() => _coverage?.HidePreview();

    // --- Хелперы ---

    private bool CheckIfTouchingRoad(Vector2Int rootPos, Vector2Int baseSize, float rotation)
    {
        if (_gridSystem == null) return false;
        Vector2Int rotatedSize = GetRotatedSize(baseSize, rotation);

        for (int x = 0; x < rotatedSize.x; x++)
        {
            for (int z = 0; z < rotatedSize.y; z++)
            {
                Vector2Int current = new Vector2Int(rootPos.x + x, rootPos.y + z);
                foreach (var offset in _neighborOffsets)
                {
                    if (_gridSystem.GetRoadTileAt((current + offset).x, (current + offset).y) != null)
                        return true;
                }
            }
        }
        return false;
    }

    private Vector2Int GetRoadAccessCell(BuildingIdentity building)
    {
        if (building == null || building.buildingData == null) return new Vector2Int(-1, -1);
        return FindNearestRoadCell(building.rootGridPosition, building.buildingData.size, building.yRotation);
    }

    private Vector2Int GetRoadAccessCellForPos(Vector2Int pos)
    {
        // Для простоты считаем, что это здание 1x1
        return FindNearestRoadCell(pos, Vector2Int.one, 0);
    }

    private Vector2Int FindNearestRoadCell(Vector2Int root, Vector2Int size, float rot)
    {
        if (_gridSystem == null) return new Vector2Int(-1, -1);
        Vector2Int rotatedSize = GetRotatedSize(size, rot);

        for (int x = 0; x < rotatedSize.x; x++)
        {
            for (int z = 0; z < rotatedSize.y; z++)
            {
                Vector2Int current = new Vector2Int(root.x + x, root.y + z);
                foreach (var offset in _neighborOffsets)
                {
                    Vector2Int nb = current + offset;
                    if (_gridSystem.GetRoadTileAt(nb.x, nb.y) != null) return nb;
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    private Vector2Int GetRotatedSize(Vector2Int size, float rotation)
    {
        if (Mathf.Abs(rotation - 90f) < 1f || Mathf.Abs(rotation - 270f) < 1f)
            return new Vector2Int(size.y, size.x);
        return size;
    }
}