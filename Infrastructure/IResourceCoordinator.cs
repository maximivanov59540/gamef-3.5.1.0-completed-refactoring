using UnityEngine;

public interface IResourceCoordinator : IGameService
{
    void RegisterSupplyRoute(MonoBehaviour producer, MonoBehaviour consumer, ResourceType resourceType);
    void UnregisterSupplyRoute(MonoBehaviour producer, MonoBehaviour consumer);
    bool IsConsumerReserved(MonoBehaviour consumer, MonoBehaviour requestingProducer);
    bool ShouldUseExclusiveReservation(MonoBehaviour producer, ResourceType resourceType);
}