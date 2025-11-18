using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Вспомогательная структура для результатов проверки потребностей.
/// </summary>
[System.Serializable]
public struct NeedResult
{
    public Need need;   // Какая потребность проверялась
    public bool isMet;  // Удовлетворена ли она
}

/// <summary>
/// Жилой дом.
/// REFACTORED: Использует ServiceLocator вместо Singleton.Instance.
/// </summary>
[RequireComponent(typeof(BuildingIdentity))]
public class Residence : MonoBehaviour
{
    [Header("=== Уровень Дома ===")]
    public PopulationTier populationTier = PopulationTier.Farmers;
    public int housingCapacity = 10;

    [Header("=== Система Потребностей ===")]
    public List<Need> basicNeeds;
    public float consumptionIntervalSeconds = 10f;

    [Header("=== Налоги ===")]
    public float baseTaxAmount = 1f;

    // --- Зависимости (Интерфейсы) ---
    private IResourceManager _resourceManager;
    private IEventManager _eventManager;
    private AuraManager _auraManager; // AuraManager пока оставим как есть (или тоже можно перевести)
    private BuildingIdentity _identity;

    // Состояние
    private float _currentTax;
    private int _currentResidents = 0;
    private List<NeedResult> _lastNeedResults = new List<NeedResult>();
    private Coroutine _consumeNeedsCoroutine;
    private bool _isInitialized = false;

    private void Start()
    {
        // 1. Внедрение зависимостей через Service Locator
        _resourceManager = ServiceLocator.Get<IResourceManager>();
        _eventManager = ServiceLocator.Get<IEventManager>();
        
        // AuraManager пока берем как синглтон (или добавьте IAuraManager по аналогии)
        _auraManager = AuraManager.Instance; 
        
        _identity = GetComponent<BuildingIdentity>();

        if (_resourceManager == null || _eventManager == null || _auraManager == null)
        {
            Debug.LogError($"[Residence] {gameObject.name}: Не удалось найти все сервисы! Отключаюсь.");
            this.enabled = false;
            return;
        }

        // 2. Регистрация в системах
        if (!_identity.isBlueprint)
        {
            // Используем новое свойство Population в IResourceManager
            _resourceManager.Population.AddHousingCapacity(populationTier, housingCapacity);
            
            if (BuildingRegistry.Instance != null)
                BuildingRegistry.Instance.RegisterResidence(this);
        }

        _isInitialized = true;
        _consumeNeedsCoroutine = StartCoroutine(ConsumeNeedsCoroutine());
    }

    private void OnDestroy()
    {
        if (!_isInitialized) return;

        if (!_identity.isBlueprint && _resourceManager != null)
        {
            _resourceManager.Population.RemoveHousingCapacity(populationTier, housingCapacity);
        }

        if (BuildingRegistry.Instance != null)
            BuildingRegistry.Instance.UnregisterResidence(this);

        if (_consumeNeedsCoroutine != null)
            StopCoroutine(_consumeNeedsCoroutine);
    }

    private IEnumerator ConsumeNeedsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(consumptionIntervalSeconds);

            if (_identity.isBlueprint) continue;

            // 1. Проверка сервисов (Аура)
            bool hasMarket = CheckServiceNeeds();

            // 2. Потребление ресурсов
            List<NeedResult> resourceResults;
            if (hasMarket)
                resourceResults = CheckAndConsumeResourceNeeds();
            else
                resourceResults = FailAllResourceNeeds();

            // 3. Применение эффектов
            ProcessResults(resourceResults);
        }
    }

    private bool CheckServiceNeeds()
    {
        // Здесь AuraManager проверяет доступность рынка
        return _auraManager.IsPositionInAura(transform.position, AuraType.Market);
    }

    private List<NeedResult> CheckAndConsumeResourceNeeds()
    {
        float intervalRatio = consumptionIntervalSeconds / 60f;
        var results = new List<NeedResult>();

        foreach (var need in basicNeeds)
        {
            if (!IsNeedUnlocked(need)) continue;

            float amountNeeded = need.amountPerMinute * intervalRatio;
            
            // Используем интерфейс IResourceManager
            bool success = _resourceManager.TakeFromStorage(need.resourceType, amountNeeded) > 0;
            
            results.Add(new NeedResult { need = need, isMet = success });
        }

        return results;
    }

    private List<NeedResult> FailAllResourceNeeds()
    {
        var results = new List<NeedResult>();
        foreach (var need in basicNeeds)
        {
            if (!IsNeedUnlocked(need)) continue;
            results.Add(new NeedResult { need = need, isMet = false });
        }
        return results;
    }

    private void ProcessResults(List<NeedResult> resourceResults)
    {
        _lastNeedResults = new List<NeedResult>(resourceResults);

        float happinessChange = 0;
        float tax = baseTaxAmount;
        int population = 0;
        int satisfied = 0;

        foreach (var res in resourceResults)
        {
            if (res.isMet)
            {
                satisfied++;
                population += res.need.populationBonus;
                happinessChange += res.need.happinessBonus;
                tax += res.need.taxBonusPerCycle;
            }
            else
            {
                happinessChange += res.need.happinessPenalty;
            }
        }

        _currentResidents = Mathf.Min(population, housingCapacity);
        _currentTax = tax;

        // Используем интерфейс IEventManager
        _eventManager.AddHappiness(happinessChange);
        
        // Обновляем население (опционально, если нужна точная цифра текущих жителей)
        // _resourceManager.Population.SetCurrentPopulation(populationTier, _currentResidents);
    }

    // Хелпер для проверки разблокировки (теперь использует IResourceManager)
    private bool IsNeedUnlocked(Need need)
    {
        if (!need.requiresUnlock) return true;
        
        int currentPop = _resourceManager.Population.GetCurrentPopulation(need.unlockPopulationTier);
        return currentPop >= need.unlockAtPopulation;
    }

    // --- Публичные методы ---

    public float GetCurrentTax() => (_identity != null && _identity.isBlueprint) ? 0 : _currentTax;

    public float GetPandemicChanceReduction()
    {
        float reduction = 0f;
        foreach (var r in _lastNeedResults)
            if (r.isMet) reduction += r.need.pandemicChanceReduction;
        return Mathf.Clamp01(reduction);
    }

    public float GetRiotChanceReduction()
    {
        float reduction = 0f;
        foreach (var r in _lastNeedResults)
            if (r.isMet) reduction += r.need.riotChanceReduction;
        return Mathf.Clamp01(reduction);
    }
}