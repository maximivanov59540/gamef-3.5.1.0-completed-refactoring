using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс для управления населением и рынком труда.
/// Раньше это были два отдельных менеджера (PopulationManager + WorkforceManager).
/// Теперь это часть данных ResourceManager.
/// </summary>
[System.Serializable]
public class PopulationData
{
    // FIX #17: Кешируем Enum.GetValues для избежания аллокаций
    private static readonly PopulationTier[] AllTiers = (PopulationTier[])System.Enum.GetValues(typeof(PopulationTier));

    // События
    public event System.Action<PopulationTier> OnPopulationChanged;
    public event System.Action OnAnyPopulationChanged;

    // --- Население (бывший PopulationManager) ---
    private Dictionary<PopulationTier, int> _currentPopulation = new Dictionary<PopulationTier, int>();
    private Dictionary<PopulationTier, int> _maxPopulation = new Dictionary<PopulationTier, int>();

    // --- Рабочая сила (бывший WorkforceManager) ---
    [Tooltip("Включить/Выключить всю систему 'Рынка Труда'")]
    public bool workforceSystemEnabled = true;

    private Dictionary<PopulationTier, int> _totalRequiredWorkforce = new Dictionary<PopulationTier, int>();
    private Dictionary<PopulationTier, int> _totalAvailableWorkforce = new Dictionary<PopulationTier, int>();
    
    // Список продюсеров для пересчета (если нужно)
    private HashSet<ResourceProducer> _allProducers = new HashSet<ResourceProducer>();

    public PopulationData()
    {
        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        foreach (PopulationTier tier in AllTiers)
        {
            _currentPopulation[tier] = 0;
            _maxPopulation[tier] = 0;
            _totalRequiredWorkforce[tier] = 0;
            _totalAvailableWorkforce[tier] = 0;
        }
    }
    
    // ==================== POPULATION LOGIC ====================

    public void AddHousingCapacity(PopulationTier tier, int amount)
    {
        if (!_maxPopulation.ContainsKey(tier)) return;

        _maxPopulation[tier] += amount;
        UpdateWorkforce(); // Жилье = Потенциальные работники

        OnPopulationChanged?.Invoke(tier);
        OnAnyPopulationChanged?.Invoke();
    }

    public void RemoveHousingCapacity(PopulationTier tier, int amount)
    {
        if (!_maxPopulation.ContainsKey(tier)) return;

        _maxPopulation[tier] = Mathf.Max(0, _maxPopulation[tier] - amount);
        UpdateWorkforce();

        OnPopulationChanged?.Invoke(tier);
        OnAnyPopulationChanged?.Invoke();
    }
    
    public void SetCurrentPopulation(PopulationTier tier, int amount)
    {
        if (!_currentPopulation.ContainsKey(tier)) return;

        _currentPopulation[tier] = Mathf.Clamp(amount, 0, _maxPopulation[tier]);
        // (В этой модели рабочая сила зависит от МАКСИМУМА (мест), но можно переделать на ТЕКУЩЕЕ)
        
        OnPopulationChanged?.Invoke(tier);
        OnAnyPopulationChanged?.Invoke();
    }

    public int GetTotalCurrentPopulation()
    {
        int total = 0;
        foreach (var kvp in _currentPopulation) total += kvp.Value;
        return total;
    }

    public int GetTotalMaxPopulation()
    {
        int total = 0;
        foreach (var kvp in _maxPopulation) total += kvp.Value;
        return total;
    }
    
    public int GetCurrentPopulation(PopulationTier tier) => _currentPopulation.GetValueOrDefault(tier, 0);

    // ==================== WORKFORCE LOGIC ====================

    public void RegisterProducer(ResourceProducer producer)
    {
        if (!workforceSystemEnabled || producer == null) return;

        if (_allProducers.Add(producer))
        {
            AddWorkforceRequirement(producer.requiredWorkerType, producer.workforceRequired);
        }
    }

    public void UnregisterProducer(ResourceProducer producer)
    {
        if (!workforceSystemEnabled || producer == null) return;

        if (_allProducers.Remove(producer))
        {
            RemoveWorkforceRequirement(producer.requiredWorkerType, producer.workforceRequired);
        }
    }

    private void AddWorkforceRequirement(PopulationTier tier, int amount)
    {
        if (_totalRequiredWorkforce.ContainsKey(tier))
            _totalRequiredWorkforce[tier] += amount;
    }

    private void RemoveWorkforceRequirement(PopulationTier tier, int amount)
    {
        if (_totalRequiredWorkforce.ContainsKey(tier))
            _totalRequiredWorkforce[tier] = Mathf.Max(0, _totalRequiredWorkforce[tier] - amount);
    }

    /// <summary>
    /// Пересчитывает доступную рабочую силу на основе жилья.
    /// </summary>
    private void UpdateWorkforce()
    {
        // Логика: 1 жилое место = 1 работник (можно усложнить, использовать _currentPopulation)
        foreach (PopulationTier tier in AllTiers)
        {
            _totalAvailableWorkforce[tier] = _maxPopulation[tier];
        }
    }

    public float GetWorkforceRatio(PopulationTier tier)
    {
        if (!workforceSystemEnabled) return 1.0f;

        int required = _totalRequiredWorkforce.GetValueOrDefault(tier, 0);
        if (required <= 0) return 1.0f;

        int available = _totalAvailableWorkforce.GetValueOrDefault(tier, 0);
        return Mathf.Clamp01((float)available / required);
    }
    
    // Для инспектора (чтобы видеть данные в ResourceManager)
    public void UpdateInspectorValues() { /* Можно добавить поля для дебага */ }
    public List<ResourceProducer> GetAllProducers()
    {
        // _allProducers у нас HashSet, поэтому создаем из него новый List
        return new List<ResourceProducer>(_allProducers);
    }
}