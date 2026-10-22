using System;
using System.Collections.Generic;

public class WorkbenchModel
{
    public IReadOnlyList<MachineConfig> Recipes { get; }
    public MachineConfig Ready { get; private set; }
    public MachineTrait ReadyTrait { get; private set; }
    public bool HasReady => Ready != null;
    
    public MachineConfig InProgress { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsCrafting => InProgress != null;
    public float Progress => IsCrafting && InProgress.craftTime > 0f ? 1f - TimeLeft / InProgress.craftTime : 0f;

    public event Action<MachineConfig> ReadyChanged;
    public event Action ProgressChanged;

    private readonly ResourceStorageModel storage;
    private readonly IReadOnlyList<MachineTrait> traits;
    private readonly float traitChance;
    private readonly Random random = new Random();

    // traits, traitChance — скрафченный станок с этим шансом получает случайное свойство (листу брони не достаётся).
    public WorkbenchModel(IReadOnlyList<MachineConfig> recipes, ResourceStorageModel storage,
                          IReadOnlyList<MachineTrait> traits = null, float traitChance = 0f)
    {
        Recipes = recipes ?? Array.Empty<MachineConfig>();
        this.storage = storage;
        this.traits = traits;
        this.traitChance = traitChance;
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

    public MachineConfig Take(out MachineTrait trait)
    {
        var machine = Ready;
        trait = ReadyTrait;
        if (machine != null) SetReady(null, null);
        TryFinish();
        return machine;
    }

    public void PutBack(MachineConfig machine, MachineTrait trait)
    {
        if (machine == null || HasReady) return;
        SetReady(machine, trait);
    }
    
    public bool TryGive(MachineConfig machine, MachineTrait trait)
    {
        if (machine == null || HasReady) return false;
        SetReady(machine, trait);
        return true;
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
        SetReady(machine, RollTrait(machine));
    }

    private MachineTrait RollTrait(MachineConfig machine)
    {
        if (machine == null || machine.action == MachineAction.Plating || traits == null || traits.Count == 0) return null;
        return random.NextDouble() < traitChance ? traits[random.Next(traits.Count)] : null;
    }

    private void SetReady(MachineConfig machine, MachineTrait trait)
    {
        Ready = machine;
        ReadyTrait = machine != null ? trait : null;
        ReadyChanged?.Invoke(machine);
    }
}
