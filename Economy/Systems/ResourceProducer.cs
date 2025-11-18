using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Производитель ресурсов.
/// REFACTORED: Использует ServiceLocator.
/// </summary>
public class ResourceProducer : MonoBehaviour
{
    [Tooltip("Данные о 'рецепте'")]
    public ResourceProductionData productionData;
    
    [Header("Рабочая Сила")]
    public PopulationTier requiredWorkerType = PopulationTier.Farmers;
    public int workforceRequired = 0;

    // --- Свойства ---
    public bool IsPaused { get; set; } = false; // Исправили сеттер ранее

    [Header("Разгон")]
    public float rampUpTimeSeconds = 60.0f;
    public float rampDownTimeSeconds = 60.0f;
    
    // --- Внутреннее состояние ---
    private Dictionary<ResourceType, ResourceCost> _inputCostLookup = new Dictionary<ResourceType, ResourceCost>();
    private float _rampUpEfficiency = 0.0f;
    private float _currentModuleBonus = 1.0f;
    private float _efficiencyModifier = 1.0f;
    private float _currentWorkforceCap = 1.0f;
    private float _cycleTimer = 0f;
    private bool _hasWarehouseAccess = false;
    private bool _initialized = false;

    // --- Зависимости (Интерфейсы) ---
    private IResourceManager _resourceManager;
    private IRoadManager _roadManager;
    
    // --- Компоненты ---
    private IBuildingIdentifiable _identity;
    private IBuildingRouting _routing;
    private IResourceReceiver _inputInv;
    private IResourceProvider _outputInv;
    private GridSystem _gridSystem;

    void Awake()
    {
        _inputInv = GetComponent<IResourceReceiver>();
        _outputInv = GetComponent<IResourceProvider>();
        _identity = GetComponent<IBuildingIdentifiable>();
        _routing = GetComponent<IBuildingRouting>();

        // Подписка на события инвентаря
        var concreteOutput = _outputInv as BuildingOutputInventory;
        if (concreteOutput != null)
        {
            concreteOutput.OnFull += PauseProduction;
            concreteOutput.OnSpaceAvailable += ResumeProduction;
        }

        if (BuildingRegistry.Instance != null && _identity != null && !_identity.isBlueprint)
        {
            BuildingRegistry.Instance.RegisterProducer(this);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeRoutine());
    }
    public bool GetHasWarehouseAccess() 
    { 
        return _hasWarehouseAccess; 
    }

    public float GetWorkforceCap() 
    { 
        return _currentWorkforceCap; 
    }

    private IEnumerator InitializeRoutine()
    {
        // Ждем инициализации Bootstrapper'а
        yield return null; 

        _gridSystem = FindFirstObjectByType<GridSystem>();
        
        // Получаем сервисы
        _resourceManager = ServiceLocator.Get<IResourceManager>();
        _roadManager = ServiceLocator.Get<IRoadManager>();

        if (_resourceManager == null || _roadManager == null || _gridSystem == null)
        {
            Debug.LogError($"[Producer] {name}: Не удалось получить сервисы. Выключаюсь.");
            this.enabled = false;
            yield break;
        }

        // Регистрируем требования к рабочей силе
        _resourceManager.Population.RegisterProducer(this);

        // Кэш рецепта
        RebuildInputCostLookup();

        // Проверка склада
        if (_routing != null)
        {
            _hasWarehouseAccess = _routing.HasOutputDestination();
        }
        else
        {
            FindWarehouseAccess();
        }

        _initialized = true;
    }

    void Update()
    {
        if (!_initialized || IsPaused || productionData == null) return;

        // 1. Проверка склада
        if (!_hasWarehouseAccess)
        {
            if (_routing != null) _hasWarehouseAccess = true;
            else 
            {
                FindWarehouseAccess();
                if (!_hasWarehouseAccess) { PauseProduction(); return; }
                else ResumeProduction();
            }
        }

        // 2. Расчет эффективности
        var concreteInput = _inputInv as BuildingInputInventory;
        bool hasInputs = (concreteInput != null) ? concreteInput.HasResources(productionData.inputCosts) : true;

        float targetRampUp = (hasInputs && _hasWarehouseAccess) ? 1.0f : 0.0f;
        float rampSpeed = (Time.deltaTime / Mathf.Max(0.01f, targetRampUp > _rampUpEfficiency ? rampUpTimeSeconds : rampDownTimeSeconds));
        _rampUpEfficiency = Mathf.MoveTowards(_rampUpEfficiency, targetRampUp, rampSpeed);

        // Получаем рабочую силу через IResourceManager
        _currentWorkforceCap = _resourceManager.Population.GetWorkforceRatio(requiredWorkerType);

        float finalEfficiency = GetFinalEfficiency();

        if (finalEfficiency <= 0.001f)
        {
            _cycleTimer = 0f;
            return;
        }

        // 3. Цикл производства
        float currentCycleTime = productionData.cycleTimeSeconds / finalEfficiency;
        _cycleTimer += Time.deltaTime;

        if (_cycleTimer >= currentCycleTime)
        {
            _cycleTimer -= currentCycleTime;
            Produce();
        }
    }

    private void Produce()
    {
        var concreteInput = _inputInv as BuildingInputInventory;
        if (concreteInput != null && !concreteInput.HasResources(productionData.inputCosts)) return;

        var concreteOutput = _outputInv as BuildingOutputInventory;
        if (concreteOutput != null && !concreteOutput.HasSpace(productionData.outputYield.amount))
        {
            PauseProduction();
            return;
        }

        if (concreteInput != null) concreteInput.ConsumeResources(productionData.inputCosts);
        if (concreteOutput != null) concreteOutput.TryAddResource(productionData.outputYield.amount);
    }

    // --- Хелперы ---

    private void FindWarehouseAccess()
    {
        // Используем IRoadManager для доступа к графу
        var roadGraph = _roadManager.GetRoadGraph();
        if (roadGraph == null) { _hasWarehouseAccess = false; return; }

        List<Vector2Int> myAccess = LogisticsPathfinder.FindAllRoadAccess(_identity.rootGridPosition, _gridSystem, roadGraph);
        if (myAccess.Count == 0) { _hasWarehouseAccess = false; return; }

        var allWarehouses = BuildingRegistry.Instance?.GetAllWarehouses();
        if (allWarehouses == null) return;

        // Простая проверка: достижим ли хотя бы один склад?
        // (Для оптимизации можно кэшировать результат)
        var distances = LogisticsPathfinder.Distances_BFS_Multi(myAccess, 500, roadGraph);
        
        bool found = false;
        foreach(var wh in allWarehouses)
        {
            var whAccess = LogisticsPathfinder.FindAllRoadAccess(wh.GetGridPosition(), _gridSystem, roadGraph);
            foreach(var entry in whAccess)
            {
                if (distances.ContainsKey(entry)) { found = true; break; }
            }
            if (found) break;
        }
        
        _hasWarehouseAccess = found;
    }
    
    // Остальные методы (UpdateProductionRate, SetEfficiency, и т.д.) остаются без изменений
    public void UpdateProductionRate(int current, int max) => _currentModuleBonus = max == 0 ? 1f : (float)current/max;
    public void SetEfficiency(float val) => _efficiencyModifier = val;
    public float GetEfficiency() => _efficiencyModifier;
    public float GetFinalEfficiency() => _rampUpEfficiency * _currentWorkforceCap * _efficiencyModifier * _currentModuleBonus;

    public void PauseProduction() { if (!IsPaused) IsPaused = true; }
    public void ResumeProduction() { if (!IsPaused) IsPaused = false; }
    
    public float GetProductionPerMinute()
    {
        float eff = GetFinalEfficiency();
        if (eff == 0) return 0f;
        return (60f / (productionData.cycleTimeSeconds / eff)) * productionData.outputYield.amount;
    }
    
    public float GetConsumptionPerMinute(ResourceType type)
    {
        if (!_inputCostLookup.TryGetValue(type, out var cost)) return 0f;
        float eff = GetFinalEfficiency();
        if (eff == 0) return 0f;
        return (60f / (productionData.cycleTimeSeconds / eff)) * cost.amount;
    }
    
    public void RefreshWarehouseAccess()
    {
        if (_routing != null) 
        {
            _routing.RefreshRoutes();
            _hasWarehouseAccess = _routing.HasOutputDestination();
        }
        else FindWarehouseAccess();
    }
    
    private void RebuildInputCostLookup()
    {
        _inputCostLookup.Clear();
        if (productionData?.inputCosts != null)
            foreach (var c in productionData.inputCosts) _inputCostLookup[c.resourceType] = c;
    }
    
    private void OnDestroy()
    {
        if (_resourceManager != null) _resourceManager.Population.UnregisterProducer(this);
        if (BuildingRegistry.Instance != null) BuildingRegistry.Instance.UnregisterProducer(this);
        
        var concreteOutput = _outputInv as BuildingOutputInventory;
        if (concreteOutput != null)
        {
            concreteOutput.OnFull -= PauseProduction;
            concreteOutput.OnSpaceAvailable -= ResumeProduction;
        }
    }
}