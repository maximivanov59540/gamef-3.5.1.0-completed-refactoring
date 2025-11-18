using UnityEngine;

public interface IBuildingIdentifiable
{
    BuildingData buildingData { get; }
    
    // 🛠 ДОБАВИЛИ set;, ЧТОБЫ MOVER МОГ МЕНЯТЬ ПОЗИЦИЮ
    Vector2Int rootGridPosition { get; set; } 
    float yRotation { get; set; }
    
    bool isBlueprint { get; set; } // Тоже добавили set
    int currentTier { get; }

    bool CanUpgradeToNextTier();
    BuildingData GetNextTierData();

    Transform transform { get; }
    GameObject gameObject { get; }
    
    // Для доступа к .enabled компонента
    bool enabled { get; set; } 
}