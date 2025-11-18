using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CartInventory))]
[RequireComponent(typeof(CartPathfinder))]
[RequireComponent(typeof(CartMovement))]
public class CartAgent : MonoBehaviour
{
    private enum State { Idle, Loading, Delivering, Unloading, Returning }
    private State _state = State.Idle;

    private CartInventory _inventory;
    private CartPathfinder _pathfinder;
    private CartMovement _movement;
    
    private Transform _homeBase;
    private IResourceProvider _homeOutput;
    private IBuildingRouting _routing;
    private Vector2Int _homeGridPos;

    private void Awake()
    {
        _inventory = GetComponent<CartInventory>();
        _pathfinder = GetComponent<CartPathfinder>();
        _movement = GetComponent<CartMovement>();
    }

    private void Start()
    {
        InitializeHomeBase();
        _pathfinder.Initialize();
        _movement.Initialize(); // Убедитесь, что в CartMovement есть этот метод
        
        _movement.OnDestinationReached += OnMoveComplete;
    }

    private void InitializeHomeBase()
    {
        _homeBase = transform.parent;
        if (!_homeBase) { enabled = false; return; }
        
        _homeOutput = _homeBase.GetComponent<IResourceProvider>();
        _routing = _homeBase.GetComponent<IBuildingRouting>();
        
        var id = _homeBase.GetComponent<BuildingIdentity>();
        _homeGridPos = id ? id.rootGridPosition : Vector2Int.zero;
    }

    private void Update()
    {
        if (_state == State.Idle)
        {
            // Логика принятия решения: есть ресурсы?
            if (_homeOutput != null && _homeOutput.GetAvailableAmount(_homeOutput.GetProvidedResourceType()) >= 1)
            {
                StartCoroutine(LoadRoutine());
            }
        }
    }

    private IEnumerator LoadRoutine()
    {
        _state = State.Loading;
        yield return new WaitForSeconds(1f); // Loading animation

        var type = _homeOutput.GetProvidedResourceType();
        float taken = _homeOutput.TryTakeResource(type, 5f);
        _inventory.AddResource(type, taken);

        // Куда везти?
        if (_routing != null && _routing.outputDestination != null)
        {
            var target = _routing.outputDestination;
            if (_pathfinder.TryFindPath(_homeGridPos, target.GetGridPosition(), out var path))
            {
                _movement.SetPath(path);
                _state = State.Delivering;
            }
            else
            {
                // Пути нет - возвращаем ресурсы
                ReturnCargo();
            }
        }
        else
        {
            ReturnCargo();
        }
    }

    private void OnMoveComplete()
    {
        if (_state == State.Delivering)
        {
            StartCoroutine(UnloadRoutine());
        }
        else if (_state == State.Returning)
        {
            _state = State.Idle;
        }
    }

    private IEnumerator UnloadRoutine()
    {
        _state = State.Unloading;
        yield return new WaitForSeconds(1f);

        var dest = _routing?.outputDestination;
        if (dest != null)
        {
            foreach(var slot in _inventory.GetSlots())
            {
                if (slot.IsEmpty) continue;
                float added = dest.TryAddResource(slot.type, slot.amount);
                slot.amount -= added;
            }
            _routing.NotifyDeliveryCompleted();
        }

        // Возврат домой
        if (_pathfinder.TryFindPath(dest.GetGridPosition(), _homeGridPos, out var path))
        {
            _movement.SetPath(path);
            _state = State.Returning;
        }
        else
        {
            // Телепорт домой (fallback)
            transform.position = _homeBase.position;
            _state = State.Idle;
        }
        
        // Очистка остатков
        _inventory.ClearAll();
    }

    private void ReturnCargo()
    {
        // Возвращаем в здание (упрощенно - просто удаляем из тележки)
        _inventory.ClearAll();
        _state = State.Idle;
    }
    
    // Прокси для визуализатора
    public System.Collections.Generic.List<Vector3> GetRemainingPathWorld() => _movement.GetRemainingPathWorld();
}