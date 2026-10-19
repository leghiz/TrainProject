using System;
using UnityEngine;

public interface IMachineHost
{
    int Hp { get; }
    int MaxHp { get; }
    void Repair(int amount);
}

public class MachineModel
{
    public MachineConfig Config { get; }
    public Vector2Int Cell { get; } 

    public float Progress => timer / Interval;
    
    public float? IntervalOverride { get; set; }
    public int Capacity { get; set; }
    private float Interval => (IntervalOverride ?? Config.workInterval) / SpeedMultiplier;
    
    public MachineTrait Trait { get; }
    private float SpeedMultiplier => Trait != null ? Trait.speedMultiplier : 1f;
    private float CostMultiplier => Trait != null ? Trait.costMultiplier : 1f;
    public bool IsRainproof => Trait != null && Trait.rainproof;
    public bool IsWorking { get; private set; } = true;   

    public event Action<bool> WorkingChanged;
    public event Action Worked;
    
    public int Loaded { get; private set; }
    public bool NeedsLoading => Config.action == MachineAction.Convert || Config.action == MachineAction.Fuel;
    public event Action<int> LoadedChanged;
    
    public int ChargeLeft { get; private set; }
    public bool HasCharge => Config.charge > 0;
    public bool IsUsedUp => HasCharge && ChargeLeft <= 0;
    public event Action<int> ChargeChanged;
    
    public int Hp { get; private set; }
    public bool IsBroken => Hp <= 0;
    public event Action<int> HpChanged;
    
    public bool IsFinished => IsUsedUp || IsBroken;

    private float timer;

    public MachineModel(MachineConfig config, Vector2Int cell, MachineTrait trait = null)
    {
        Config = config;
        Cell = cell;
        Trait = trait;
        ChargeLeft = config.charge;
        Capacity = config.capacity;
        Hp = config.maxHp;
    }

    public void Damage(int amount)
    {
        if (IsBroken || amount <= 0) return;
        Hp = Mathf.Max(0, Hp - amount);
        HpChanged?.Invoke(Hp);
    }
    
    public void Tick(float deltaTime, IMachineHost host, ResourceStorageModel storage)
    {
        timer += deltaTime;
        if (timer < Interval) return;
        timer -= Interval;

        bool worked = Work(host, storage);
        SetWorking(worked);
        if (worked) Worked?.Invoke();
    }

    public int Load(ResourceStorageModel storage, int amount)
    {
        if (!NeedsLoading || amount <= 0) return 0;

        int room = Capacity > 0 ? Capacity - Loaded : int.MaxValue;
        int take = Mathf.Min(amount, Mathf.Min(storage.Get(Config.resource), room));
        if (take <= 0 || !storage.TrySpend(Config.resource, take)) return 0;

        Loaded += take;
        LoadedChanged?.Invoke(Loaded);
        return take;
    }
    
    public int Fill(int amount)
    {
        if (!NeedsLoading || amount <= 0) return 0;

        int room = Capacity > 0 ? Capacity - Loaded : int.MaxValue;
        int put = Mathf.Min(amount, room);
        if (put <= 0) return 0;

        Loaded += put;
        LoadedChanged?.Invoke(Loaded);
        return put;
    }
    private bool Work(IMachineHost host, ResourceStorageModel storage)
    {
        switch (Config.action)
        {
            case MachineAction.Extract:
                if (!(host is WagonModel wagon)) return false;
                int extracted = wagon.Extract(Config.resource, Config.amount, Config.wagonDamage * CostMultiplier);
                if (extracted <= 0) return false;
                storage.Add(Config.resource, extracted);
                return true;

            case MachineAction.Repair:
                int missing = host.MaxHp - host.Hp;
                if (missing <= 0 || IsUsedUp) return false;
                int give = Mathf.Min(Config.amount, missing);
                if (HasCharge) give = Mathf.Min(give, ChargeLeft);
                host.Repair(give);
                if (HasCharge)
                {
                    ChargeLeft -= give;
                    ChargeChanged?.Invoke(ChargeLeft);
                }
                return true;

            case MachineAction.Convert:
                if (!UseLoaded(LoadedCost())) return false;
                storage.Add(Config.outputResource, Config.outputAmount);
                return true;

            case MachineAction.Fuel:
                return UseLoaded(LoadedCost());

            case MachineAction.None:
                return true;

            default:
                return false;
        }
    }
    
    private int LoadedCost() => Mathf.Max(1, Mathf.RoundToInt(Config.amount * CostMultiplier));

    private bool UseLoaded(int amount)
    {
        if (Loaded < amount) return false;
        Loaded -= amount;
        LoadedChanged?.Invoke(Loaded);
        return true;
    }

    private void SetWorking(bool working)
    {
        if (IsWorking == working) return;
        IsWorking = working;
        WorkingChanged?.Invoke(working);
    }
}
