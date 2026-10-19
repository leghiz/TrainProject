using System;
using System.Collections.Generic;
using UnityEngine;

public class WagonModel : IMachineHost
{
    public WagonConfig Config { get; }
    public int Hp { get; private set; }
    public int MaxHp => Config.maxHp;
    public int Wood { get; private set; }   
    public int Scrap { get; private set; }  
    public bool IsDestroyed => Hp <= 0;

    public event Action<int> HpChanged;   
    public event Action Destroyed;        
    private readonly MachineModel[,] grid;
    private readonly List<MachineModel> machines = new List<MachineModel>();
    public IReadOnlyList<MachineModel> Machines => machines;

    public event Action<MachineModel> MachinePlaced;
    public event Action<MachineModel> MachineRemoved;
    public event Action ResourcesChanged;
    
    private readonly CargoBoxModel[,] boxGrid;
    private readonly List<CargoBoxModel> boxes = new List<CargoBoxModel>();
    public IReadOnlyList<CargoBoxModel> Boxes => boxes;
    public event Action<CargoBoxModel> BoxPlaced;
    public event Action<CargoBoxModel> BoxRemoved;
    public event Action<CargoBoxModel> BoxTaken;

    public WagonCondition Condition { get; }
    
    public float Weight
    {
        get
        {
            float weight = Config.size.x * Config.size.y * (Condition != null ? Condition.weightMultiplier : 1f);
            foreach (var box in boxes) weight += box.Config.weight;
            return weight;
        }
    }
    
    public WagonModel(WagonConfig config, WagonCondition condition = null, float woodMultiplier = 1f, float scrapMultiplier = 1f)
    {
        Config = config;
        Condition = condition;
        Hp = config.maxHp;
        float resources = condition != null ? condition.resourceMultiplier : 1f;
        Wood = Mathf.RoundToInt(config.wood * resources * woodMultiplier);
        Scrap = Mathf.RoundToInt(config.scrap * resources * scrapMultiplier);
        grid = new MachineModel[config.size.x, config.size.y];
        boxGrid = new CargoBoxModel[config.size.x, config.size.y];
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
    public int Extract(ResourceType type, int amount, float damage)
    {
        if (IsDestroyed || amount <= 0) return 0;

        int extracted;
        switch (type)
        {
            case ResourceType.Wood:
                extracted = Mathf.Min(amount, Wood);
                Wood -= extracted;
                break;
            case ResourceType.Scrap:
                extracted = Mathf.Min(amount, Scrap);
                Scrap -= extracted;
                break;
            default:
                return 0;
        }

        if (extracted > 0)
        {
            TakeExtractionDamage(damage); 
            ResourcesChanged?.Invoke();   
        }
        return extracted;
    }
    
    private float pendingExtractionDamage;

    private void TakeExtractionDamage(float hp)
    {
        pendingExtractionDamage += hp;
        int whole = Mathf.FloorToInt(pendingExtractionDamage);
        if (whole <= 0) return;
        pendingExtractionDamage -= whole;
        Damage(whole);
    }
    
    public void Tick(float deltaTime, ResourceStorageModel storage)
    {
        foreach (var machine in machines)
        {
            if (IsDestroyed) break;
            machine.Tick(deltaTime, this, storage);
        }
        
        RemoveFinishedMachines();
    }
    
    public void TakeRain(int damage)
    {
        if (damage <= 0) return;
        foreach (var machine in machines)
            if (!machine.IsRainproof) machine.Damage(damage);
        RemoveFinishedMachines();

        foreach (var box in boxes) box.Damage(damage);
        for (int i = boxes.Count - 1; i >= 0; i--)
            if (boxes[i].IsBroken) RemoveBox(boxes[i]);
    }

    public bool CanPlaceBox(CargoBoxConfig box, Vector2Int cell) => IsAreaFree(cell, box.size);
    
    public CargoBoxModel TryPlaceBox(CargoBoxConfig config, Vector2Int cell)
    {
        if (!CanPlaceBox(config, cell)) return null;

        var box = new CargoBoxModel(config, cell);
        for (int x = 0; x < config.size.x; x++)
        for (int y = 0; y < config.size.y; y++)
            boxGrid[cell.x + x, cell.y + y] = box;

        boxes.Add(box);
        BoxPlaced?.Invoke(box);
        return box;
    }
    
    public void RemoveBox(CargoBoxModel box)
    {
        if (!TakeBoxOut(box)) return;
        BoxRemoved?.Invoke(box);
    }

    // Снять ящик целиком (для перестановки): посылка при этом не проваливается.
    public bool TakeBox(CargoBoxModel box)
    {
        if (!TakeBoxOut(box)) return false;
        BoxTaken?.Invoke(box);
        return true;
    }

    public bool TryPlaceExistingBox(CargoBoxModel box, Vector2Int cell)
    {
        if (!CanPlaceBox(box.Config, cell)) return false;

        for (int x = 0; x < box.Config.size.x; x++)
        for (int y = 0; y < box.Config.size.y; y++)
            boxGrid[cell.x + x, cell.y + y] = box;

        boxes.Add(box);
        box.MoveTo(cell, this);
        BoxPlaced?.Invoke(box);
        return true;
    }

    private bool TakeBoxOut(CargoBoxModel box)
    {
        if (!boxes.Remove(box)) return false;
        for (int x = 0; x < box.Config.size.x; x++)
        for (int y = 0; y < box.Config.size.y; y++)
            boxGrid[box.Cell.x + x, box.Cell.y + y] = null;
        return true;
    }
    
    private void RemoveFinishedMachines()
    {
        for (int i = machines.Count - 1; i >= 0; i--)
        {
            if (machines[i].IsFinished) RemoveMachine(machines[i]);
        }
    }
    
    // Игрок разобрал станок: он снимается, на склад возвращается половина его цены.
    public bool Dismantle(MachineModel machine, ResourceStorageModel storage)
    {
        if (!machines.Contains(machine)) return false;
        RemoveMachine(machine);
        MachineRefund.GiveHalf(machine.Config, storage);
        return true;
    }

    private void RemoveMachine(MachineModel machine)
    {
        for (int x = 0; x < machine.Config.size.x; x++)
        for (int y = 0; y < machine.Config.size.y; y++)
            grid[machine.Cell.x + x, machine.Cell.y + y] = null;

        machines.Remove(machine);
        MachineRemoved?.Invoke(machine);
    }

    public bool CanPlace(MachineConfig machine, Vector2Int cell) =>
        machine.action != MachineAction.Plating && IsAreaFree(cell, machine.size);

    private bool IsAreaFree(Vector2Int cell, Vector2Int size)
    {
        if (IsDestroyed) return false;

        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            var c = new Vector2Int(cell.x + x, cell.y + y);
            if (!IsInside(c)) return false;
            if (grid[c.x, c.y] != null || boxGrid[c.x, c.y] != null) return false;
        }
        return true;
    }

    // Снять станок с вагона целиком (для перестановки): хп, загрузка и свойство остаются при нём.
    public bool TakeMachine(MachineModel machine)
    {
        if (!machines.Contains(machine)) return false;
        RemoveMachine(machine);
        return true;
    }

    public bool TryPlaceExisting(MachineModel machine, Vector2Int cell)
    {
        if (!CanPlace(machine.Config, cell)) return false;

        machine.MoveTo(cell);
        for (int x = 0; x < machine.Config.size.x; x++)
        for (int y = 0; y < machine.Config.size.y; y++)
            grid[cell.x + x, cell.y + y] = machine;

        machines.Add(machine);
        MachinePlaced?.Invoke(machine);
        return true;
    }

    public bool TryPlace(MachineConfig machine, Vector2Int cell, MachineTrait trait = null)
    {
        if (!CanPlace(machine, cell)) return false;

        var placed = new MachineModel(machine, cell, trait);
        for (int x = 0; x < machine.size.x; x++)
        for (int y = 0; y < machine.size.y; y++)
            grid[cell.x + x, cell.y + y] = placed;

        machines.Add(placed);
        MachinePlaced?.Invoke(placed);
        return true;
    }
    
    public MachineModel GetMachineAt(Vector2Int cell) => IsInside(cell) ? grid[cell.x, cell.y] : null;

    public CargoBoxModel GetBoxAt(Vector2Int cell) => IsInside(cell) ? boxGrid[cell.x, cell.y] : null;

    private bool IsInside(Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < Config.size.x && c.y < Config.size.y;
}
