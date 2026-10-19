using System;
using System.Collections.Generic;

public class WorkbenchModel
{
    public IReadOnlyList<MachineConfig> Recipes { get; }
    public MachineConfig Ready { get; private set; }
    public bool HasReady => Ready != null;

    public event Action<MachineConfig> ReadyChanged;

    private readonly ResourceStorageModel storage;

    public WorkbenchModel(IReadOnlyList<MachineConfig> recipes, ResourceStorageModel storage)
    {
        Recipes = recipes ?? Array.Empty<MachineConfig>();
        this.storage = storage;
    }

    public bool CanCraft(MachineConfig machine)
    {
        if (machine == null || HasReady) return false;
        if (!IsRecipe(machine)) return false;
        return storage.Has(ResourceType.Wood, machine.craftWood)
            && storage.Has(ResourceType.Scrap, machine.craftScrap)
            && storage.Has(ResourceType.Ingot, machine.craftIngots);
    }
    
    public bool TryCraft(MachineConfig machine)
    {
        if (!CanCraft(machine)) return false;

        storage.TrySpend(ResourceType.Wood, machine.craftWood);
        storage.TrySpend(ResourceType.Scrap, machine.craftScrap);
        storage.TrySpend(ResourceType.Ingot, machine.craftIngots);
        SetReady(machine);
        return true;
    }

    public MachineConfig Take()
    {
        var machine = Ready;
        if (machine != null) SetReady(null);
        return machine;
    }

    public void PutBack(MachineConfig machine)
    {
        if (machine == null || HasReady) return;
        SetReady(machine);
    }

    public bool IsRecipe(MachineConfig machine)
    {
        foreach (var recipe in Recipes)
            if (recipe == machine) return true;
        return false;
    }

    private void SetReady(MachineConfig machine)
    {
        Ready = machine;
        ReadyChanged?.Invoke(machine);
    }
}
