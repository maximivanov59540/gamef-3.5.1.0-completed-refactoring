using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// --- ВОТ ЭТОТ БЛОК Я ПРОПУСТИЛ В ПРОШЛЫЙ РАЗ ---
public enum InputMode
{
    None,
    Building,
    Moving,
    Deleting,
    Upgrading,
    Copying,
    Selecting,
    GroupCopying,
    GroupMoving,
    RoadBuilding,
    PlacingModule,
    RoadOperation
}
// ------------------------------------------------

/// <summary>
/// Главный контроллер ввода.
/// REFACTORED: Использует Service Locator для получения зависимостей.
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    public static PlayerInputController Instance { get; private set; }
    public static InputMode CurrentInputMode { get; private set; } = InputMode.None;

    // --- Зависимости (теперь приватные и заполняются из Локатора) ---
    private IResourceManager _resourceManager;
    private IRoadManager _roadManager;
    private IMoneyManager _moneyManager; 

    // --- Ссылки на другие системы ---
    [Header("Scene References")]
    [SerializeField] private GridSystem gridSystem;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private SelectionManager _selectionManager;
    [SerializeField] private NotificationManager _notificationManager;
    
    // Хендлеры
    [SerializeField] private MassBuildHandler _massBuildHandler;
    [SerializeField] private GroupOperationHandler _groupOperationHandler;
    [SerializeField] private RoadBuildHandler _roadBuildHandler;
    [SerializeField] private RoadOperationHandler _roadOperationHandler;
    [SerializeField] private AuraManager _auraManager;

    // Состояния
    private Dictionary<InputMode, IInputState> _states;
    private IInputState _currentState;

    // Кэш зон
    private ZonedArea[] _cachedZones;

    public SelectionManager Selection => _selectionManager;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        // Авто-поиск ссылок на сцене (Fallback)
        if (!gridSystem) gridSystem = FindFirstObjectByType<GridSystem>();
        if (!uiManager) uiManager = FindFirstObjectByType<UIManager>();
        if (!buildingManager) buildingManager = FindFirstObjectByType<BuildingManager>();
        if (!_selectionManager) _selectionManager = FindFirstObjectByType<SelectionManager>();
        if (!_notificationManager) _notificationManager = FindFirstObjectByType<NotificationManager>();
        
        // Хендлеры
        if (!_massBuildHandler) _massBuildHandler = FindFirstObjectByType<MassBuildHandler>();
        if (!_groupOperationHandler) _groupOperationHandler = FindFirstObjectByType<GroupOperationHandler>();
        if (!_roadBuildHandler) _roadBuildHandler = FindFirstObjectByType<RoadBuildHandler>();
        if (!_roadOperationHandler) _roadOperationHandler = FindFirstObjectByType<RoadOperationHandler>();
        if (!_auraManager) _auraManager = FindFirstObjectByType<AuraManager>();
    }

    void Start()
    {
        // 1. Получаем основные сервисы через Service Locator
        _resourceManager = ServiceLocator.Get<IResourceManager>();
        _roadManager = ServiceLocator.Get<IRoadManager>();
        _moneyManager = ServiceLocator.Get<IMoneyManager>();

        if (_resourceManager == null) Debug.LogWarning("PlayerInputController: IResourceManager не найден в ServiceLocator.");

        // 2. Кэширование зон (для подсветки)
        RefreshZonedAreaCache();

        // 3. Инициализация состояний (с внедрением новых зависимостей)
        InitializeStates();
        
        // 4. Запуск дефолтного состояния
        SetMode(InputMode.None);
    }

    private void InitializeStates()
    {
        _states = new Dictionary<InputMode, IInputState>();
        
        // Получаем дополнительные сервисы для UI и Ауры
        // (Используем локальные переменные, так как они нужны только для создания состояний)
        var noteMgr = ServiceLocator.Get<INotificationManager>();
        var auraMgr = ServiceLocator.Get<IAuraManager>();

        // fallback на случай запуска без Bootstrapper (опционально)
        if (noteMgr == null) noteMgr = FindFirstObjectByType<NotificationManager>();
        if (auraMgr == null) auraMgr = FindFirstObjectByType<AuraManager>();

        // --- Инициализация Dictionary с состояниями ---

        // State_None
        _states[InputMode.None] = new State_None(this, noteMgr, gridSystem, uiManager, _selectionManager, buildingManager);
        
        // State_Building: Теперь принимает IAuraManager
        _states[InputMode.Building] = new State_Building(this, noteMgr, buildingManager, _massBuildHandler, (AuraManager)auraMgr); 
        
        // State_Moving
        _states[InputMode.Moving] = new State_Moving(this, noteMgr, buildingManager, _selectionManager, _groupOperationHandler, gridSystem);
        
        // State_Deleting: Использует IRoadManager (кастуем к RoadManager, если конструктор еще не обновлен на интерфейс)
        _states[InputMode.Deleting] = new State_Deleting(this, noteMgr, buildingManager, gridSystem, uiManager, _selectionManager, (RoadManager)_roadManager); 
        
        // State_Upgrading: Использует IResourceManager
        _states[InputMode.Upgrading] = new State_Upgrading(this, noteMgr, buildingManager, gridSystem, _selectionManager, (RoadManager)_roadManager, (ResourceManager)_resourceManager);

        // Остальные состояния
        _states[InputMode.Copying] = new State_Copying(this, noteMgr, buildingManager, gridSystem, _selectionManager, _groupOperationHandler);
        _states[InputMode.Selecting] = new State_Selecting(this, noteMgr, _selectionManager);
        
        _states[InputMode.GroupCopying] = new State_GroupCopying(this, noteMgr, _groupOperationHandler);
        _states[InputMode.GroupMoving] = new State_GroupMoving(this, noteMgr, _groupOperationHandler);
        
        _states[InputMode.RoadBuilding] = new State_RoadBuilding(this, noteMgr, _roadBuildHandler);
        _states[InputMode.RoadOperation] = new State_RoadOperation(this, noteMgr, _roadOperationHandler, buildingManager);
        
        _states[InputMode.PlacingModule] = new State_PlacingModule(this, gridSystem, buildingManager, noteMgr);
    }

    void Update()
    {
        _currentState?.OnUpdate();
    }

    public void SetMode(InputMode newMode)
    {
        _currentState?.OnExit();

        if (_states.TryGetValue(newMode, out var state))
        {
            _currentState = state;
            CurrentInputMode = newMode;
        }
        else
        {
            Debug.LogError($"Попытка войти в неизвестный режим: {newMode}");
            _currentState = _states[InputMode.None];
            CurrentInputMode = InputMode.None;
        }

        // Управление подсветкой зон
        if (newMode != InputMode.Building && newMode != InputMode.PlacingModule)
        {
            HideZoneHighlights();
        }

        _currentState.OnEnter();
        BuildOrchestrator.Instance?.OnModeChanged(CurrentInputMode);
    }

    // --- Публичные методы ---

    public void EnterPlacingModuleMode(ModularBuilding targetFarm, BuildingData moduleToBuild)
    {
        if (targetFarm == null || moduleToBuild == null) return;
        
        SetMode(InputMode.PlacingModule);
        
        if (_currentState is State_PlacingModule moduleState)
        {
            moduleState.OnEnter(targetFarm, moduleToBuild);
        }
    }

    public void RefreshZonedAreaCache()
    {
#if UNITY_2022_2_OR_NEWER
        _cachedZones = FindObjectsByType<ZonedArea>(FindObjectsSortMode.None);
#else
        _cachedZones = FindObjectsOfType<ZonedArea>();
#endif
    }

    // --- Хелперы ---
    
    public bool IsPointerOverUI() => EventSystem.current.IsPointerOverGameObject();
    public Vector3 GetMouseWorldPosition() => GridSystem.MouseWorldPosition;

    private void HideZoneHighlights()
    {
        if (_cachedZones == null) return;
        foreach (var zone in _cachedZones)
        {
            if (zone != null) zone.HideSlotHighlights();
        }
    }
}