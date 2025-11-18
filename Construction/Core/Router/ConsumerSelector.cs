using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Логика выбора следующего потребителя (Round-Robin).
/// </summary>
public class ConsumerSelector
{
    private readonly MonoBehaviour _context;
    private int _deliveryCount = 0;
    private readonly int _deliveriesBeforeRotation;

    public ConsumerSelector(MonoBehaviour context, int deliveriesBeforeRotation)
    {
        _context = context;
        _deliveriesBeforeRotation = deliveriesBeforeRotation;
    }

    public void NotifyDelivery()
    {
        _deliveryCount++;
    }

    public bool ShouldRotate()
    {
        if (_deliveryCount >= _deliveriesBeforeRotation)
        {
            _deliveryCount = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Ищет следующего потребителя в списке доступных.
    /// </summary>
    public IResourceReceiver RotateToNextConsumer(IResourceReceiver current, ResourceType type)
    {
        var allInputs = BuildingRegistry.Instance?.GetAllInputs();
        if (allInputs == null) return current;

        var candidates = new List<BuildingInputInventory>();
        
        // Фильтруем подходящих
        foreach (var input in allInputs)
        {
            if (input.gameObject == _context.gameObject) continue;
            
            if (input.requiredResources != null)
            {
                foreach (var slot in input.requiredResources)
                {
                    if (slot.resourceType == type)
                    {
                        candidates.Add(input);
                        break;
                    }
                }
            }
        }

        if (candidates.Count <= 1) return current;

        // Находим текущего
        int idx = -1;
        for(int i=0; i<candidates.Count; i++)
        {
            if ((object)candidates[i] == (object)current) 
            {
                idx = i; 
                break; 
            }
        }

        // Берем следующего
        int nextIdx = (idx + 1) % candidates.Count;
        return candidates[nextIdx];
    }
}