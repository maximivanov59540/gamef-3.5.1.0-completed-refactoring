using UnityEngine;

public interface IEventManager : IGameService
{
    // Счастье
    void AddHappiness(float amount);
    void SetHappiness(float value);
    float GetCurrentHappiness();
    float GetNormalizedHappiness();
    float GetEventChanceModifier();

    // События
    void RegisterBuilding(EventAffected building);
    void UnregisterBuilding(EventAffected building);
    
    // Управление
    void UnlockPandemics();
    void UnlockRiots();
}