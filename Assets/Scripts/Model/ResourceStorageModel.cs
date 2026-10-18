using System;
using System.Collections.Generic;

public class ResourceStorageModel
{
    private readonly Dictionary<ResourceType, int> amounts = new Dictionary<ResourceType, int>();

    public event Action Changed;

    public int Get(ResourceType type) => amounts.TryGetValue(type, out var value) ? value : 0;

    public bool Has(ResourceType type, int amount) => Get(type) >= amount;

    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        amounts[type] = Get(type) + amount;
        Changed?.Invoke();
    }
    public bool TrySpend(ResourceType type, int amount)
    {
        if (amount < 0 || !Has(type, amount)) return false;
        amounts[type] = Get(type) - amount;
        Changed?.Invoke();
        return true;
    }
}
