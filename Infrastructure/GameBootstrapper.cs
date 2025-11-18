using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [Header("Менеджеры сцены")]
    [SerializeField] private ResourceManager _resourceManager;
    [SerializeField] private RoadManager _roadManager;
    [SerializeField] private MoneyManager _moneyManager;
    [SerializeField] private EventManager _eventManager;
    [SerializeField] private ResourceCoordinator _resourceCoordinator;
    [SerializeField] private AuraManager _auraManager;
    [SerializeField] private NotificationManager _notificationManager;

    private void Awake()
    {
        // 1. Очищаем старые связи (на случай перезагрузки сцены)
        ServiceLocator.Clear();

        Debug.Log("[GameBootstrapper] Начало регистрации сервисов...");

        // 2. Регистрируем сервисы
        // Важно: Менеджеры должны реализовывать соответствующие интерфейсы!
        
        if (_resourceManager != null)
        {
            // Мы регистрируем _resourceManager КАК IResourceManager
            ServiceLocator.Register<IResourceManager>(_resourceManager);
        }
        else Debug.LogError("[GameBootstrapper] ResourceManager не назначен!");

        if (_roadManager != null)
        {
            ServiceLocator.Register<IRoadManager>(_roadManager);
        }
        else Debug.LogError("[GameBootstrapper] RoadManager не назначен!");

        if (_moneyManager != null)
        {
            ServiceLocator.Register<IMoneyManager>(_moneyManager);
        }
        else Debug.LogError("[GameBootstrapper] MoneyManager не назначен!");

        Debug.Log("[GameBootstrapper] Регистрация завершена.");

        if (_resourceCoordinator) ServiceLocator.Register<IResourceCoordinator>(_resourceCoordinator);
        Debug.Log("[GameBootstrapper] Готово.");

        if (_auraManager) ServiceLocator.Register<IAuraManager>(_auraManager);
        else Debug.LogError("[GameBootstrapper] AuraManager не назначен!");

        if (_notificationManager) ServiceLocator.Register<INotificationManager>(_notificationManager);
        else Debug.LogError("[GameBootstrapper] NotificationManager не назначен!");
    }
}