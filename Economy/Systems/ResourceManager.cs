using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Глобальное хранилище ресурсов.
/// Refactored: Реализует IResourceManager для Service Locator.
/// </summary>
public class ResourceManager : MonoBehaviour, IResourceManager
{
    // Синглтон оставляем для обратной совместимости (пока не отрефакторим всё),
    // но внутри новых скриптов лучше использовать IResourceManager.
    public static ResourceManager Instance { get; private set; }

    [Header("Settings")]
    public float baseResourceLimit = 50f;

    // --- State ---
    // Словарь публичный, но лучше работать через методы
    public Dictionary<ResourceType, StorageData> GlobalStorage = new Dictionary<ResourceType, StorageData>();

    // --- Sub-Systems ---
    [SerializeField] 
    private PopulationData _populationData;

    // Реализация свойства из интерфейса
    public PopulationData Population => _populationData;

    // События
    public event System.Action<ResourceType> OnResourceChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _populationData = new PopulationData();
        InitializeResources();
    }

    private void InitializeResources()
    {
        GlobalStorage.Clear();
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            GlobalStorage.Add(type, new StorageData(0, baseResourceLimit));
        }

        // Стартовые ресурсы
        AddToStorage(ResourceType.Wood, 100f);
        AddToStorage(ResourceType.Stone, 50f);
    }

    // --- Реализация IResourceManager ---

    public float GetResourceAmount(ResourceType type)
    {
        return GlobalStorage.TryGetValue(type, out var data) ? data.currentAmount : 0f;
    }

    public float AddToStorage(ResourceType type, float amount)
    {
        if (!GlobalStorage.TryGetValue(type, out var slot)) return 0;

        float space = slot.maxAmount - slot.currentAmount;
        float toAdd = Mathf.Min(amount, space);

        if (toAdd > 0)
        {
            slot.currentAmount += toAdd;
            OnResourceChanged?.Invoke(type);
        }
        return toAdd;
    }

    public float TakeFromStorage(ResourceType type, float amount)
    {
        if (!GlobalStorage.TryGetValue(type, out var slot)) return 0;

        float toTake = Mathf.Min(amount, slot.currentAmount);

        if (toTake > 0)
        {
            slot.currentAmount -= toTake;
            OnResourceChanged?.Invoke(type);
        }
        return toTake;
    }

    public bool CanAfford(BuildingData data)
    {
        if (data == null || data.costs == null) return true;
        return CanAfford(data.costs);
    }

    public bool CanAfford(List<ResourceCost> costs)
    {
        if (costs == null) return true;
        foreach (var cost in costs)
        {
            if (GetResourceAmount(cost.resourceType) < cost.amount) return false;
        }
        return true;
    }

    public void SpendResources(BuildingData data) => SpendResources(data.costs);

    public void SpendResources(List<ResourceCost> costs)
    {
        if (costs == null) return;
        foreach (var cost in costs)
        {
            TakeFromStorage(cost.resourceType, cost.amount);
        }
    }

    public void IncreaseGlobalLimit(float amount)
    {
        foreach (var kvp in GlobalStorage) kvp.Value.maxAmount += amount;
        OnResourceChanged?.Invoke(ResourceType.Wood); // Force UI update
    }
    public float GetResourceLimit(ResourceType type)
    {
        if (GlobalStorage.TryGetValue(type, out var data))
        {
            return data.maxAmount;
        }
        return 0f;
    }
}