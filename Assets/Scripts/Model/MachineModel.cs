using System;
using UnityEngine;

public class MachineModel
{
    public MachineConfig Config { get; }
    public Vector2Int Cell { get; } 

    public float Progress => timer / Config.workInterval; 
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

    private float timer;

    public MachineModel(MachineConfig config, Vector2Int cell)
    {
        Config = config;
        Cell = cell;
        ChargeLeft = config.charge;
    }
    
    public void Tick(float deltaTime, WagonModel wagon, ResourceStorageModel storage)
    {
        timer += deltaTime;
        if (timer < Config.workInterval) return;
        timer -= Config.workInterval;

        bool worked = Work(wagon, storage);
        SetWorking(worked);
        if (worked) Worked?.Invoke();
    }

    public int Load(ResourceStorageModel storage, int amount)
    {
        if (!NeedsLoading || amount <= 0) return 0;

        int take = Mathf.Min(amount, storage.Get(Config.resource));
        if (take <= 0 || !storage.TrySpend(Config.resource, take)) return 0;

        Loaded += take;
        LoadedChanged?.Invoke(Loaded);
        return take;
    }
    private bool Work(WagonModel wagon, ResourceStorageModel storage)
    {
        switch (Config.action)
        {
            case MachineAction.Extract:
                int extracted = wagon.Extract(Config.resource, Config.amount, Config.wagonDamage);
                if (extracted <= 0) return false;
                storage.Add(Config.resource, extracted);
                return true;

            case MachineAction.Repair:
                int missing = wagon.Config.maxHp - wagon.Hp;
                if (missing <= 0 || IsUsedUp) return false;
                int give = Mathf.Min(Config.amount, missing);
                if (HasCharge) give = Mathf.Min(give, ChargeLeft);
                wagon.Repair(give);
                if (HasCharge)
                {
                    ChargeLeft -= give;
                    ChargeChanged?.Invoke(ChargeLeft);
                }
                return true;

            case MachineAction.Convert:
                if (!UseLoaded(Config.amount)) return false;
                storage.Add(Config.outputResource, Config.outputAmount);
                return true;

            case MachineAction.Fuel:
                return UseLoaded(Config.amount);

            default:
                return false;
        }
    }

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
