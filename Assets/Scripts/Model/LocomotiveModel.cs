using System;
using UnityEngine;

public class LocomotiveModel : IMachineHost
{
    public LocomotiveConfig Config { get; }
    public int Hp { get; private set; }
    public int MaxHp => Config.maxHp;
    public bool IsDestroyed => Hp <= 0;

    public event Action<int> HpChanged;
    public event Action Destroyed;
    
    public MachineModel Boiler { get; }
    public MachineModel Workbench { get; }
    public MachineModel Welder { get; private set; }

    public event Action<MachineModel> MachinePlaced;
    public event Action<MachineModel> MachineRemoved;
    
    public int BoilerLevel { get; private set; } = 1;
    public BoilerLevelSettings Level => Config.boilerLevels[Mathf.Clamp(BoilerLevel - 1, 0, Config.boilerLevels.Length - 1)];
    public bool HasLevels => Config.boilerLevels != null && Config.boilerLevels.Length > 0;
    public bool CanUpgradeBoiler => HasLevels && BoilerLevel < Config.boilerLevels.Length;
    public event Action<int> BoilerLevelChanged;

    public bool HasFuel => Boiler != null && Boiler.Loaded > 0;

    private float pendingWear;

    public LocomotiveModel(LocomotiveConfig config)
    {
        Config = config;
        Hp = config.maxHp;
        if (config.boiler != null) Boiler = new MachineModel(config.boiler, Vector2Int.zero);
        if (config.workbench != null) Workbench = new MachineModel(config.workbench, Vector2Int.zero);
        ApplyBoilerLevel();
        Boiler?.Fill(config.startFuel);
    }

    public bool UpgradeBoiler()
    {
        if (!CanUpgradeBoiler) return false;
        BoilerLevel++;
        ApplyBoilerLevel();
        BoilerLevelChanged?.Invoke(BoilerLevel);
        return true;
    }
    
    private void ApplyBoilerLevel()
    {
        if (Boiler == null || !HasLevels) return;
        Boiler.IntervalOverride = Level.fuelInterval;
        Boiler.Capacity = Level.tank;
    }

    public void ApplyWear(float deltaTime, float multiplier = 1f)
    {
        if (IsDestroyed || !HasLevels) return;
        pendingWear += Level.wearPerSecond * multiplier * deltaTime;
        int whole = Mathf.FloorToInt(pendingWear);
        if (whole <= 0) return;
        pendingWear -= whole;
        Damage(whole);
    }

    public void Damage(int amount)
    {
        if (IsDestroyed || amount <= 0) return;

        Hp = Mathf.Max(0, Hp - amount);
        HpChanged?.Invoke(Hp);

        if (IsDestroyed) Destroyed?.Invoke();
    }

    public void Repair(int amount)
    {
        if (IsDestroyed || amount <= 0) return;

        Hp = Mathf.Min(Config.maxHp, Hp + amount);
        HpChanged?.Invoke(Hp);
    }
    
    public bool CanPlaceWelder(MachineConfig machine) =>
        !IsDestroyed && Welder == null && machine != null
        && machine.action == MachineAction.Repair && machine.size == Vector2Int.one;

    public bool TryPlaceWelder(MachineConfig machine)
    {
        if (!CanPlaceWelder(machine)) return false;
        Welder = new MachineModel(machine, Vector2Int.zero);
        MachinePlaced?.Invoke(Welder);
        return true;
    }

    public void Tick(float deltaTime, ResourceStorageModel storage)
    {
        if (IsDestroyed) return;

        Boiler?.Tick(deltaTime, this, storage);
        Workbench?.Tick(deltaTime, this, storage);
        Welder?.Tick(deltaTime, this, storage);

        if (Welder != null && Welder.IsUsedUp)
        {
            var usedUp = Welder;
            Welder = null;
            MachineRemoved?.Invoke(usedUp);
        }
    }
}
