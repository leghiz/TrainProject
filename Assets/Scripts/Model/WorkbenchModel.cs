using System;
using System.Collections.Generic;

public class WorkbenchModel
{
    public IReadOnlyList<MachineConfig> Recipes { get; }
    public MachineConfig Ready { get; private set; }
    public bool HasReady => Ready != null;
    
    public MachineConfig InProgress { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsCrafting => InProgress != null;
    public float Progress => IsCrafting && InProgress.craftTime > 0f ? 1f - TimeLeft / InProgress.craftTime : 0f;

    public event Action<MachineConfig> ReadyChanged;
    public event Action ProgressChanged;

    private readonly ResourceStorageModel storage;

    public WorkbenchModel(IReadOnlyList<MachineConfig> recipes, ResourceStorageModel storage)
    {
        Recipes = recipes ?? Array.Empty<MachineConfig>();
        this.storage = storage;
    }
    
    public bool CanCraft(MachineConfig machine)
    {
        if (machine == null || HasReady || IsCrafting) return false;
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
        InProgress = machine;
        TimeLeft = machine.craftTime;
        ProgressChanged?.Invoke();
        TryFinish();
        return true;
    }
    
    public void Tick(float deltaTime)
    {
        if (!IsCrafting) return;
        TimeLeft = Math.Max(0f, TimeLeft - deltaTime);
        ProgressChanged?.Invoke();
        TryFinish();
    }

    public MachineConfig Take()
    {
        var machine = Ready;
        if (machine != null) SetReady(null);
        TryFinish();
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
    
    private void TryFinish()
    {
        if (!IsCrafting || TimeLeft > 0f || HasReady) return;
        var machine = InProgress;
        InProgress = null;
        ProgressChanged?.Invoke();
        SetReady(machine);
    }

    private void SetReady(MachineConfig machine)
    {
        Ready = machine;
        ReadyChanged?.Invoke(machine);
    }
}
