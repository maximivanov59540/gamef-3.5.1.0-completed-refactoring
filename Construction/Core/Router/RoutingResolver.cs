using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Логика поиска маршрутов: находит ближайшие склады и потребителей.
/// </summary>
public class RoutingResolver
{
    private readonly MonoBehaviour _context;
    private IRoadManager _roadManager;

    public RoutingResolver(MonoBehaviour context)
    {
        _context = context;
        _roadManager = ServiceLocator.Get<IRoadManager>(); // Внедрение зависимости
    }

    public IResourceReceiver FindNearestConsumerForOutput(ResourceType producedType)
    {
        var allInputs = BuildingRegistry.Instance?.GetAllInputs();
        if (allInputs == null) return null;

        var candidates = new List<BuildingInputInventory>();
        Vector3 myPos = _context.transform.position;

        foreach (var input in allInputs)
        {
            if (input.gameObject == _context.gameObject) continue;
            if (input.AcceptsResource(producedType))
            {
                candidates.Add(input);
            }
        }

        // Сортируем по дистанции (можно улучшить до поиска пути)
        candidates.Sort((a, b) => Vector3.Distance(myPos, a.transform.position)
            .CompareTo(Vector3.Distance(myPos, b.transform.position)));

        return candidates.Count > 0 ? candidates[0] : null;
    }

    public IResourceProvider FindNearestProducerForInput(ResourceType neededType)
    {
        var allOutputs = BuildingRegistry.Instance?.GetAllOutputs();
        if (allOutputs == null) return null;

        var candidates = new List<BuildingOutputInventory>();
        Vector3 myPos = _context.transform.position;

        foreach (var output in allOutputs)
        {
            if (output.gameObject == _context.gameObject) continue;
            if (output.GetProvidedResourceType() == neededType)
            {
                candidates.Add(output);
            }
        }

        candidates.Sort((a, b) => Vector3.Distance(myPos, a.transform.position)
            .CompareTo(Vector3.Distance(myPos, b.transform.position)));

        return candidates.Count > 0 ? candidates[0] : null;
    }

    public Warehouse FindNearestWarehouse()
    {
        var allWarehouses = BuildingRegistry.Instance?.GetAllWarehouses();
        if (allWarehouses == null || allWarehouses.Count == 0) return null;

        Warehouse best = null;
        float minDst = float.MaxValue;
        Vector3 myPos = _context.transform.position;

        foreach (var wh in allWarehouses)
        {
            float d = Vector3.Distance(myPos, wh.transform.position);
            if (d < minDst)
            {
                minDst = d;
                best = wh;
            }
        }
        return best;
    }
}