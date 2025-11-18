using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простой Service Locator для Unity.
/// Позволяет регистрировать и получать сервисы, избавляя от Singleton.Instance зависимостей.
/// </summary>
public static class ServiceLocator
{
    // Словарь для хранения зарегистрированных сервисов
    private static readonly Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();

    /// <summary>
    /// Регистрирует сервис в локаторе.
    /// Обычно вызывается в Awake() Bootstrapper'а.
    /// </summary>
    /// <typeparam name="T">Тип интерфейса сервиса (например, IResourceManager)</typeparam>
    /// <param name="service">Экземпляр сервиса</param>
    public static void Register<T>(T service) where T : class, IGameService
    {
        var type = typeof(T);
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] Сервис типа {type.Name} уже зарегистрирован! Перезапись.");
            _services[type] = service;
        }
        else
        {
            _services.Add(type, service);
            Debug.Log($"[ServiceLocator] Зарегистрирован сервис: {type.Name}");
        }
    }

    /// <summary>
    /// Получает сервис из локатора.
    /// </summary>
    /// <typeparam name="T">Тип интерфейса сервиса</typeparam>
    /// <returns>Экземпляр сервиса или null, если не найден</returns>
    public static T Get<T>() where T : class, IGameService
    {
        var type = typeof(T);
        if (_services.TryGetValue(type, out var service))
        {
            return service as T;
        }

        Debug.LogError($"[ServiceLocator] Сервис типа {type.Name} не найден! Убедитесь, что он зарегистрирован в Bootstrapper.");
        return null;
    }

    /// <summary>
    /// Очищает все зарегистрированные сервисы.
    /// Полезно при перезагрузке сцены или выходе из игры.
    /// </summary>
    public static void Clear()
    {
        _services.Clear();
        Debug.Log("[ServiceLocator] Все сервисы очищены.");
    }
}