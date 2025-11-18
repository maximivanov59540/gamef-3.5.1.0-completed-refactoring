using UnityEngine;

[RequireComponent(typeof(BuildingIdentity))]
public class BuildingResourceRouting : MonoBehaviour, IBuildingRouting
{
    [Header("Настройки")]
    [SerializeField] private Transform _outputDestinationTransform;
    [SerializeField] private Transform _inputSourceTransform;
    [SerializeField] private bool _enableRoundRobin = true;
    [SerializeField] private int _deliveriesBeforeRotation = 1;

    // Компоненты логики
    private RoutingResolver _resolver;
    private ConsumerSelector _selector;
    
    // Состояние
    public IResourceReceiver outputDestination { get; private set; }
    public IResourceProvider inputSource { get; private set; }

    private void Awake()
    {
        _resolver = new RoutingResolver(this);
        _selector = new ConsumerSelector(this, _deliveriesBeforeRotation);
    }

    private void Start() => RefreshRoutes();

    // --- Public API ---

    public void RefreshRoutes()
    {
        // 1. Output
        if (_outputDestinationTransform != null)
        {
            outputDestination = _outputDestinationTransform.GetComponent<IResourceReceiver>();
        }
        else
        {
            // Пытаемся найти потребителя
            var producer = GetComponent<IResourceProvider>();
            if (producer != null)
            {
                outputDestination = _resolver.FindNearestConsumerForOutput(producer.GetProvidedResourceType());
            }

            // Fallback: Склад
            if (outputDestination == null)
                outputDestination = _resolver.FindNearestWarehouse();
        }

        // 2. Input
        if (_inputSourceTransform != null)
        {
            inputSource = _inputSourceTransform.GetComponent<IResourceProvider>();
        }
        else
        {
            // Пытаемся найти производителя
            var consumer = GetComponent<IResourceReceiver>();
            if (consumer is BuildingInputInventory inp && inp.requiredResources.Count > 0)
            {
                inputSource = _resolver.FindNearestProducerForInput(inp.requiredResources[0].resourceType);
            }

            // Fallback: Склад
            if (inputSource == null)
                inputSource = _resolver.FindNearestWarehouse();
        }
    }

    public void NotifyDeliveryCompleted()
    {
        if (!_enableRoundRobin || outputDestination == null) return;

        _selector.NotifyDelivery();
        if (_selector.ShouldRotate())
        {
            var provider = GetComponent<IResourceProvider>();
            if (provider != null)
            {
                // Пытаемся найти следующего (Round Robin)
                var next = _selector.RotateToNextConsumer(outputDestination, provider.GetProvidedResourceType());
                if (next != null && next != outputDestination)
                {
                    outputDestination = next;
                    Debug.Log($"[Routing] Переключение на следующего потребителя: {next}");
                }
            }
        }
    }

    // Интерфейс IBuildingRouting
    public Transform outputDestinationTransform { get => _outputDestinationTransform; set { _outputDestinationTransform = value; RefreshRoutes(); } }
    public Transform inputSourceTransform { get => _inputSourceTransform; set { _inputSourceTransform = value; RefreshRoutes(); } }
    public bool IsConfigured() => outputDestination != null;
    public bool HasOutputDestination() => outputDestination != null;
    public bool HasInputSource() => inputSource != null;
    public void SetOutputDestination(Transform dest) => outputDestinationTransform = dest;
    public void SetInputSource(Transform src) => inputSourceTransform = src;
    
    // Регистрация в реестре (для оптимизации)
    private void OnEnable() => BuildingRegistry.Instance?.RegisterRouting(this);
    private void OnDisable() => BuildingRegistry.Instance?.UnregisterRouting(this);
}