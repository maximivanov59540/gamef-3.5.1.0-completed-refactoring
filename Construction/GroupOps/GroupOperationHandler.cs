using System.Collections.Generic;
using UnityEngine;

public class GroupOperationHandler : MonoBehaviour
{
    public static GroupOperationHandler Instance { get; private set; }

    [Header("Ссылки")]
    [SerializeField] private GridSystem _gridSystem;
    [SerializeField] private PlayerInputController _inputController;
    [SerializeField] private BuildingManager _buildingManager;
    
    // Используем интерфейс вместо конкретного класса
    private IRoadManager _roadManager;
    private NotificationManager _notificationManager;

    // Пул призраков
    private readonly List<GameObject> _ghostPool = new();
    private int _ghostPoolIndex = 0;

    // Структуры данных
    private struct GroupOffset
    {
        public BuildingData data;
        public Vector2Int offset;
        public float yRotationDelta;
        public bool isBlueprint;
    }

    private struct RoadOffset
    {
        public RoadData roadData;
        public Vector2Int offset;
    }

    private struct LiftedBuildingData
    {
        public GameObject gameObject;
        public GroupOffset offset;
        public Vector2Int originalPosition;
        public float originalRotation;
    }

    // Состояние
    private readonly List<GroupOffset> _currentGroupOffsets_Copy = new();
    private readonly List<RoadOffset> _currentRoadOffsets_Copy = new();
    private readonly List<LiftedBuildingData> _liftedBuildingData = new();
    private readonly List<RoadOffset> _liftedRoadOffsets = new();

    private Vector2Int _anchorGridPos;
    private float _anchorRotation;
    private float _currentGroupRotation = 0f;
    private bool _canPlaceGroup = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        _notificationManager = FindFirstObjectByType<NotificationManager>();
        _roadManager = ServiceLocator.Get<IRoadManager>(); // Внедрение зависимости
        if (_gridSystem == null) _gridSystem = FindFirstObjectByType<GridSystem>();
    }

    // --------- Массовое копирование ---------

    public void StartMassCopy(HashSet<BuildingIdentity> selection, List<Vector2Int> roadCells)
    {
        ClearAllLists();
        _currentGroupRotation = 0f;

        // 1. Находим якорь (левый нижний угол)
        BuildingIdentity anchorId = GetAnchorBuilding(selection);
        if (anchorId == null) return;

        _anchorGridPos = anchorId.rootGridPosition;
        _anchorRotation = anchorId.yRotation;

        // 2. Собираем здания
        foreach (var id in selection)
        {
            _currentGroupOffsets_Copy.Add(new GroupOffset
            {
                data = id.buildingData,
                offset = id.rootGridPosition - _anchorGridPos,
                yRotationDelta = id.yRotation - _anchorRotation,
                isBlueprint = id.isBlueprint
            });
        }

        // 3. Собираем дороги
        var allRoadPositions = new HashSet<Vector2Int>(roadCells);
        allRoadPositions.UnionWith(CollectRoadsUnderBuildings(selection));

        foreach (var roadPos in allRoadPositions)
        {
            RoadTile roadTile = _gridSystem.GetRoadTileAt(roadPos.x, roadPos.y);
            if (roadTile != null && roadTile.roadData != null)
            {
                _currentRoadOffsets_Copy.Add(new RoadOffset
                {
                    roadData = roadTile.roadData,
                    offset = roadPos - _anchorGridPos
                });
            }
        }

        _inputController.SetMode(InputMode.GroupCopying);
    }

    public void UpdateGroupPreview(Vector2Int mouseGridPos)
    {
        _ghostPoolIndex = 0;
        _canPlaceGroup = true;
        float cellSize = _gridSystem.GetCellSize();

        // Призраки зданий
        foreach (var entry in _currentGroupOffsets_Copy)
        {
            Vector2Int rotatedOffset = GroupMath.RotateVector(entry.offset, _currentGroupRotation);
            Vector2Int finalPos = mouseGridPos + rotatedOffset;
            float finalRot = (_currentGroupRotation + entry.yRotationDelta) % 360f;
            Vector2Int finalSize = GroupMath.GetRotatedSize(entry.data.size, finalRot);

            GameObject ghost = GetGhostFromPool(entry.data);
            PositionGhost(ghost, finalPos, finalRot, finalSize, cellSize);

            bool canPlace = _gridSystem.CanBuildAt(finalPos, finalSize);
            _buildingManager.SetBuildingVisuals(ghost, entry.isBlueprint ? VisualState.Blueprint : VisualState.Ghost, canPlace);

            if (!canPlace) _canPlaceGroup = false;
        }

        // Призраки дорог
        foreach (var roadEntry in _currentRoadOffsets_Copy)
        {
            Vector2Int rotatedOffset = GroupMath.RotateVector(roadEntry.offset, _currentGroupRotation);
            Vector2Int finalPos = mouseGridPos + rotatedOffset;

            GameObject ghost = GetGhostFromPool(roadEntry.roadData);
            PositionRoadGhost(ghost, finalPos, cellSize);

            bool canPlace = !_gridSystem.IsCellOccupied(finalPos.x, finalPos.y);
            _buildingManager.SetBuildingVisuals(ghost, VisualState.Ghost, canPlace);

            if (!canPlace) _canPlaceGroup = false;
        }

        HideUnusedGhosts();
    }

    public void ExecutePlaceGroupCopy()
    {
        if (!_canPlaceGroup)
        {
            _notificationManager?.ShowNotification("Место занято!");
            return;
        }

        Vector2Int anchorPos = GridSystem.MouseGridPosition;
        if (anchorPos.x == -1) return;

        // Строим дороги
        foreach (var roadEntry in _currentRoadOffsets_Copy)
        {
            Vector2Int finalPos = anchorPos + GroupMath.RotateVector(roadEntry.offset, _currentGroupRotation);
            // Проверка занятости уже была в UpdateGroupPreview
            if (!_gridSystem.IsCellOccupied(finalPos.x, finalPos.y))
                _roadManager.PlaceRoad(finalPos, roadEntry.roadData);
        }

        // Строим здания
        bool blueprintMode = _buildingManager.IsBlueprintModeActive;
        foreach (var entry in _currentGroupOffsets_Copy)
        {
            Vector2Int finalPos = anchorPos + GroupMath.RotateVector(entry.offset, _currentGroupRotation);
            float finalRot = (_currentGroupRotation + entry.yRotationDelta) % 360f;
            
            // Используем метод из BuildingManager (который делегирует Placer'у)
            _buildingManager.PlaceBuildingFromOrder(entry.data, finalPos, finalRot, blueprintMode);
        }

        _notificationManager?.ShowNotification("Группа скопирована.");
    }

    // --------- Массовое перемещение ---------

    public void StartMassMove(HashSet<BuildingIdentity> selection, List<Vector2Int> roadCells)
    {
        ClearAllLists();

        BuildingIdentity anchorId = GetAnchorBuilding(selection);
        if (anchorId == null) return;

        _anchorGridPos = anchorId.rootGridPosition;
        _anchorRotation = anchorId.yRotation;

        // Сбор и удаление дорог
        var allRoadPositions = new HashSet<Vector2Int>(roadCells);
        allRoadPositions.UnionWith(CollectRoadsUnderBuildings(selection));

        foreach (var roadPos in allRoadPositions)
        {
            RoadTile roadTile = _gridSystem.GetRoadTileAt(roadPos.x, roadPos.y);
            if (roadTile != null && roadTile.roadData != null)
            {
                _liftedRoadOffsets.Add(new RoadOffset
                {
                    roadData = roadTile.roadData,
                    offset = roadPos - _anchorGridPos
                });
                _roadManager.RemoveRoad(roadPos);
            }
        }

        // Сбор и "поднятие" зданий
        foreach (var id in selection)
        {
            GameObject lifted = _gridSystem.PickUpBuilding(id.rootGridPosition.x, id.rootGridPosition.y);
            if (lifted == null) continue;

            _liftedBuildingData.Add(new LiftedBuildingData
            {
                gameObject = lifted,
                originalPosition = id.rootGridPosition,
                originalRotation = id.yRotation,
                offset = new GroupOffset
                {
                    data = id.buildingData,
                    offset = id.rootGridPosition - _anchorGridPos,
                    yRotationDelta = id.yRotation - _anchorRotation,
                    isBlueprint = id.isBlueprint
                }
            });

            _buildingManager.SetBuildingVisuals(lifted, VisualState.Ghost, true);
            BuildOrchestrator.Instance?.PauseProduction(lifted, true);
        }
        
        _inputController.SetMode(InputMode.GroupMoving);
    }
    
    public void UpdateGroupMovePreview(Vector2Int mouseGridPos)
    {
        _canPlaceGroup = true;
        float cellSize = _gridSystem.GetCellSize();

        foreach (var liftedData in _liftedBuildingData)
        {
            GameObject go = liftedData.gameObject;
            GroupOffset entry = liftedData.offset;

            Vector2Int rotatedOffset = GroupMath.RotateVector(entry.offset, _currentGroupRotation);
            Vector2Int finalPos = mouseGridPos + rotatedOffset;
            float finalRot = (_currentGroupRotation + entry.yRotationDelta) % 360f;
            Vector2Int finalSize = GroupMath.GetRotatedSize(entry.data.size, finalRot);

            PositionGhost(go, finalPos, finalRot, finalSize, cellSize);

            // Используем BuildingManager для проверки, так как он имеет доступ к Validator
            _buildingManager.CheckPlacementValidity(go, entry.data, finalPos);
            
            if (!_buildingManager.CanPlace) _canPlaceGroup = false;
        }
    }

    public void PlaceGroupMove()
    {
        if (!_canPlaceGroup)
        {
            _notificationManager?.ShowNotification("Место занято!");
            return;
        }

        Vector2Int anchorPos = GridSystem.MouseGridPosition;
        if (anchorPos.x == -1) return;

        // Дороги
        foreach (var roadEntry in _liftedRoadOffsets)
        {
            Vector2Int finalPos = anchorPos + GroupMath.RotateVector(roadEntry.offset, _currentGroupRotation);
            _roadManager.PlaceRoad(finalPos, roadEntry.roadData);
        }

        // Здания
        foreach (var liftedData in _liftedBuildingData)
        {
            GameObject go = liftedData.gameObject;
            var id = go.GetComponent<IBuildingIdentifiable>(); // Интерфейс!

            Vector2Int rotatedOffset = GroupMath.RotateVector(liftedData.offset.offset, _currentGroupRotation);
            Vector2Int finalPos = anchorPos + rotatedOffset;
            float finalRot = (_currentGroupRotation + liftedData.offset.yRotationDelta) % 360f;
            Vector2Int finalSize = GroupMath.GetRotatedSize(id.buildingData.size, finalRot);

            id.rootGridPosition = finalPos;
            id.yRotation = finalRot;

            _gridSystem.OccupyCells(id as BuildingIdentity, finalSize);
            
            bool isBlueprint = liftedData.offset.isBlueprint;
            _buildingManager.SetBuildingVisuals(go, isBlueprint ? VisualState.Blueprint : VisualState.Real, true);
            
            if (!isBlueprint) BuildOrchestrator.Instance?.PauseProduction(go, false);
        }

        _notificationManager?.ShowNotification("Группа перемещена.");
        ClearAllLists();
        _inputController.SetMode(InputMode.None);
    }

    // --- Helpers ---

    private BuildingIdentity GetAnchorBuilding(HashSet<BuildingIdentity> selection)
    {
        BuildingIdentity anchor = null;
        int minSum = int.MaxValue;
        foreach (var id in selection)
        {
            int sum = id.rootGridPosition.x + id.rootGridPosition.y;
            if (sum < minSum) { minSum = sum; anchor = id; }
        }
        return anchor;
    }

    private HashSet<Vector2Int> CollectRoadsUnderBuildings(HashSet<BuildingIdentity> buildings)
    {
        var roadPositions = new HashSet<Vector2Int>();
        foreach (var b in buildings)
        {
            if (b == null) continue;
            Vector2Int size = GroupMath.GetRotatedSize(b.buildingData.size, b.yRotation);
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(b.rootGridPosition.x + x, b.rootGridPosition.y + y);
                if (_gridSystem.GetRoadTileAt(pos.x, pos.y) != null) roadPositions.Add(pos);
            }
        }
        return roadPositions;
    }

    private void PositionGhost(GameObject ghost, Vector2Int pos, float rot, Vector2Int size, float cellSize)
    {
        Vector3 worldPos = _gridSystem.GetWorldPosition(pos.x, pos.y);
        worldPos.x += (size.x * cellSize) / 2f;
        worldPos.z += (size.y * cellSize) / 2f;
        ghost.transform.position = worldPos;
        ghost.transform.rotation = Quaternion.Euler(0, rot, 0);
    }

    private void PositionRoadGhost(GameObject ghost, Vector2Int pos, float cellSize)
    {
        Vector3 worldPos = _gridSystem.GetWorldPosition(pos.x, pos.y);
        worldPos.x += cellSize / 2f;
        worldPos.z += cellSize / 2f;
        worldPos.y += 0.01f;
        ghost.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(90, 0, 0));
    }

    // Пул и управление режимом
    public void RotateGroupPreview() => _currentGroupRotation = (_currentGroupRotation + 90f) % 360f;
    
    public void CancelGroupOperation() => QuietCancel();
    public void CancelAndExitMode() { QuietCancel(); _inputController.SetMode(InputMode.None); }

    public void QuietCancel()
    {
        // Если отменяем перемещение - возвращаем всё на место
        if (_liftedBuildingData.Count > 0)
        {
            foreach (var r in _liftedRoadOffsets)
                _roadManager.PlaceRoad(_anchorGridPos + r.offset, r.roadData);

            foreach (var l in _liftedBuildingData)
            {
                GameObject go = l.gameObject;
                if (go == null) continue;
                var id = go.GetComponent<BuildingIdentity>();
                
                Vector2Int size = GroupMath.GetRotatedSize(id.buildingData.size, l.originalRotation);
                PositionGhost(go, l.originalPosition, l.originalRotation, size, _gridSystem.GetCellSize());
                
                id.rootGridPosition = l.originalPosition;
                id.yRotation = l.originalRotation;
                
                _gridSystem.OccupyCells(id, size);
                _buildingManager.SetBuildingVisuals(go, id.isBlueprint ? VisualState.Blueprint : VisualState.Real, true);
                if (!id.isBlueprint) BuildOrchestrator.Instance?.PauseProduction(go, false);
            }
        }
        
        ClearAllLists();
        HideUnusedGhosts();
    }

    private void ClearAllLists()
    {
        _ghostPoolIndex = 0;
        _currentGroupOffsets_Copy.Clear();
        _currentRoadOffsets_Copy.Clear();
        _liftedBuildingData.Clear();
        _liftedRoadOffsets.Clear();
    }
    private GameObject GetGhostFromPool(BuildingData data)
{
    GameObject ghost;
    if (_ghostPoolIndex < _ghostPool.Count)
    {
        ghost = _ghostPool[_ghostPoolIndex];
    }
    else
    {
        ghost = Instantiate(data.buildingPrefab, transform);
        ghost.layer = LayerMask.NameToLayer("Ghost");
        ghost.tag = "Untagged"; 

        var producer = ghost.GetComponent<ResourceProducer>();
        if (producer != null) producer.enabled = false;
        var identity = ghost.GetComponent<IBuildingIdentifiable>();
        if (identity != null) identity.enabled = false;
        
        // --- УМНАЯ НАСТРОЙКА ФИЗИКИ ---
        bool hasConcaveMesh = false;
        var colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            if (col is MeshCollider meshCol && !meshCol.convex)
            {
                hasConcaveMesh = true;
                col.enabled = false; // Выключаем вогнутые коллайдеры
            }
            else
            {
                col.isTrigger = true; // Остальные (Box, Sphere, Convex) делаем триггерами
            }
        }

        // Добавляем Rigidbody, ТОЛЬКО если нет вогнутых коллайдеров
        // (Это нужно для GhostBuildingCollider в режиме ПЕРЕМЕЩЕНИЯ)
        if (!hasConcaveMesh)
        {
            var rb = ghost.GetComponent<Rigidbody>();
            if (rb == null) rb = ghost.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        // --- КОНЕЦ УМНОЙ НАСТРОЙКИ ---
        
        _ghostPool.Add(ghost);
    }

    ghost.SetActive(true);
    _ghostPoolIndex++;
    return ghost;
}
    private GameObject GetGhostFromPool(RoadData data)
    {
        GameObject ghost;
        if (_ghostPoolIndex < _ghostPool.Count) ghost = _ghostPool[_ghostPoolIndex];
        else
        {
            ghost = Instantiate(data.roadPrefab, transform);
            CleanupGhost(ghost);
            _ghostPool.Add(ghost);
        }
        ghost.SetActive(true);
        _ghostPoolIndex++;
        return ghost;
    }
    
    private void CleanupGhost(GameObject ghost)
    {
        ghost.layer = LayerMask.NameToLayer("Ghost");
        ghost.tag = "Untagged";
        var prod = ghost.GetComponent<ResourceProducer>();
        if(prod) prod.enabled = false;
        var id = ghost.GetComponent<BuildingIdentity>();
        if(id) id.enabled = false;
        foreach(var c in ghost.GetComponentsInChildren<Collider>()) c.isTrigger = true;
        var rb = ghost.GetComponent<Rigidbody>();
        if(!rb) ghost.AddComponent<Rigidbody>().isKinematic = true;
    }
    
    public void HideGhosts() => HideUnusedGhosts();
    private void HideUnusedGhosts()
    {
        for (int i = _ghostPoolIndex; i < _ghostPool.Count; i++) _ghostPool[i].SetActive(false);
    }
}