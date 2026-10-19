using System;
using System.Collections.Generic;
using UnityEngine;

public class PlateModel
{
    public MachineConfig Config { get; }
    public int Slot { get; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }

    public PlateModel(MachineConfig config, int slot)
    {
        Config = config;
        Slot = slot;
    }
}

public class PlatingModel
{
    private readonly List<PlateModel> plates = new List<PlateModel>();
    public IReadOnlyList<PlateModel> Plates => plates;

    public int SlotCount { get; }
    public bool HasPlates => plates.Count > 0;

    public int TotalHp
    {
        get
        {
            int sum = 0;
            foreach (var plate in plates) sum += plate.Hp;
            return sum;
        }
    }

    public int TotalMaxHp
    {
        get
        {
            int sum = 0;
            foreach (var plate in plates) sum += plate.MaxHp;
            return sum;
        }
    }

    public event Action<PlateModel> PlateAdded;
    public event Action<PlateModel> PlateRemoved;
    public event Action Changed;

    public PlatingModel(int slotCount)
    {
        SlotCount = slotCount;
    }

    public bool IsSlotFree(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return false;
        foreach (var plate in plates) if (plate.Slot == slot) return false;
        return true;
    }

    public bool CanAdd(MachineConfig plate, int slot) =>
        plate != null && plate.action == MachineAction.Plating && IsSlotFree(slot);

    public bool TryAdd(MachineConfig config, int slot)
    {
        if (!CanAdd(config, slot)) return false;

        var plate = new PlateModel(config, slot);
        plates.Add(plate);
        Rescale();
        plate.Hp = plate.MaxHp;

        PlateAdded?.Invoke(plate);
        Changed?.Invoke();
        return true;
    }

    public int Damage(int amount)
    {
        if (amount <= 0 || !HasPlates) return amount;

        while (amount > 0 && plates.Count > 0)
        {
            var last = plates[plates.Count - 1];
            int taken = Mathf.Min(amount, last.Hp);
            last.Hp -= taken;
            amount -= taken;

            if (last.Hp <= 0)
            {
                plates.RemoveAt(plates.Count - 1);
                Rescale();
                PlateRemoved?.Invoke(last);
            }
        }

        Changed?.Invoke();
        return amount;
    }

    private void Rescale()
    {
        int count = plates.Count;
        foreach (var plate in plates)
        {
            int newMax = plate.Config.maxHp * count;
            if (plate.MaxHp > 0 && plate.Hp > 0)
                plate.Hp = Mathf.Max(1, Mathf.RoundToInt((float)plate.Hp * newMax / plate.MaxHp));
            plate.MaxHp = newMax;
        }
    }
}
