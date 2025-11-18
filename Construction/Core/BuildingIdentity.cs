using UnityEngine;

/// <summary>
/// Компонент идентификации здания в сетке.
/// Реализует IBuildingIdentifiable для уменьшения coupling.
/// </summary>
public class BuildingIdentity : MonoBehaviour, IBuildingIdentifiable
{
    // 🛠 ИСПРАВЛЕНИЕ: Превращаем поля в Свойства (Properties), чтобы удовлетворить Интерфейс.
    // Атрибут [field: SerializeField] заставляет Unity показывать их в Инспекторе.

    [field: SerializeField] 
    public BuildingData buildingData { get; set; }

    [field: SerializeField] 
    public Vector2Int rootGridPosition { get; set; }

    [field: SerializeField] 
    public float yRotation { get; set; } = 0f;

    [field: SerializeField] 
    public bool isBlueprint { get; set; } = false;

    [field: Header("Tier System")]
    [field: Tooltip("Текущий уровень этого конкретного здания (1, 2, 3...)")]
    [field: SerializeField] 
    public int currentTier { get; set; } = 1;

    // --- Кеширование (без изменений) ---
    
    [HideInInspector] public ResourceProducer[] cachedProducers;
    [HideInInspector] public Collider[] cachedColliders;

    void Awake()
    {
        if (buildingData != null && currentTier == 1)
        {
            currentTier = buildingData.currentTier;
        }

        CacheComponents();

        if (BuildingRegistry.Instance != null)
        {
            BuildingRegistry.Instance.RegisterBuilding(this);
        }
    }

    public void CacheComponents()
    {
        if (cachedProducers == null)
            cachedProducers = GetComponentsInChildren<ResourceProducer>(true);

        if (cachedColliders == null)
            cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    void OnDestroy()
    {
        if (BuildingRegistry.Instance != null)
        {
            BuildingRegistry.Instance.UnregisterBuilding(this);
        }
    }

    public bool CanUpgradeToNextTier()
    {
        return buildingData != null && buildingData.CanUpgrade() && !isBlueprint;
    }

    public BuildingData GetNextTierData()
    {
        return buildingData != null ? buildingData.nextTier : null;
    }
}