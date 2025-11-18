using UnityEngine;

/// <summary>
/// Отвечает ТОЛЬКО за проверки: можно ли строить, хватает ли денег, занята ли клетка.
/// </summary>
public class BuildingValidator : MonoBehaviour
{
    private GridSystem _gridSystem;
    
    // Используем интерфейсы для Service Locator (как планировали в Части 2)
    private IResourceManager _resourceManager;
    private IMoneyManager _moneyManager;

    public void Initialize()
    {
        _gridSystem = Object.FindFirstObjectByType<GridSystem>();
        
        // Пытаемся получить через локатор, если он уже настроен.
        // Если нет — используем синглтоны как запасной вариант (чтобы код не падал сейчас)
        _resourceManager = ServiceLocator.Get<IResourceManager>() ?? (IResourceManager)ResourceManager.Instance;
        _moneyManager = ServiceLocator.Get<IMoneyManager>() ?? (IMoneyManager)MoneyManager.Instance;
    }

    public bool CanAfford(BuildingData data)
    {
        if (data == null) return false;

        // Проверка денег
        if (_moneyManager != null && !_moneyManager.CanAffordMoney(data.moneyCost)) 
            return false;
        
        // Проверка ресурсов
        if (_resourceManager != null && !_resourceManager.CanAfford(data)) 
            return false;
        
        return true;
    }

    /// <summary>
    /// Проверяет, свободна ли территория в сетке (GridSystem).
    /// </summary>
    public bool IsAreaClear(Vector2Int rootPos, Vector2Int size)
    {
        if (_gridSystem == null) return false;
        return _gridSystem.CanBuildAt(rootPos, size);
    }

    /// <summary>
    /// Проверяет физические коллизии (например, с юнитами или другими объектами),
    /// используя компонент GhostBuildingCollider на призраке.
    /// </summary>
    public bool CheckGhostCollision(GameObject ghost)
    {
        if (ghost == null) return false;
        
        var col = ghost.GetComponent<GhostBuildingCollider>();
        
        // Если компонента нет — считаем, что коллизий нет (true)
        // Если компонент есть — спрашиваем у него IsColliding (должно быть false для успеха)
        return col == null || !col.IsColliding();
    }
}