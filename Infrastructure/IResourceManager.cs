using System.Collections.Generic;

public interface IResourceManager : IGameService
{
    float GetResourceAmount(ResourceType type);
    float AddToStorage(ResourceType type, float amount);
    float TakeFromStorage(ResourceType type, float amount);
    
    bool CanAfford(BuildingData data);
    bool CanAfford(List<ResourceCost> costs);
    void SpendResources(BuildingData data);
    void SpendResources(List<ResourceCost> costs);
    
    void IncreaseGlobalLimit(float amount);

    // Доступ к данным населения
    PopulationData Population { get; }
    
    // События
    event System.Action<ResourceType> OnResourceChanged;
}