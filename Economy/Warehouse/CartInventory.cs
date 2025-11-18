using UnityEngine;
using System.Collections.Generic;

public class CartInventory : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public ResourceType type;
        public float amount;
        public const float MAX = 5f;
        public bool IsEmpty => amount <= 0;
        public bool IsFull => amount >= MAX;
        public float Space => MAX - amount;
    }

    [SerializeField] private Slot[] _slots = new Slot[3];

    public void Awake()
    {
        for(int i=0; i<_slots.Length; i++) _slots[i] = new Slot();
    }

    public float AddResource(ResourceType type, float amount)
    {
        float remaining = amount;
        
        // 1. Fill existing
        foreach(var slot in _slots)
        {
            if (!slot.IsEmpty && slot.type == type && !slot.IsFull)
            {
                float toAdd = Mathf.Min(remaining, slot.Space);
                slot.amount += toAdd;
                remaining -= toAdd;
                if (remaining <= 0) return amount;
            }
        }
        
        // 2. Fill empty
        if (remaining > 0)
        {
            foreach(var slot in _slots)
            {
                if (slot.IsEmpty)
                {
                    slot.type = type;
                    float toAdd = Mathf.Min(remaining, slot.Space);
                    slot.amount += toAdd;
                    remaining -= toAdd;
                    if (remaining <= 0) return amount;
                }
            }
        }
        return amount - remaining;
    }

    public void ClearAll()
    {
        foreach(var slot in _slots) { slot.amount = 0; slot.type = ResourceType.None; }
    }

    public bool IsEmpty()
    {
        foreach(var slot in _slots) if (!slot.IsEmpty) return false;
        return true;
    }
    
    public IEnumerable<Slot> GetSlots() => _slots;
}