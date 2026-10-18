using System;
using System.Collections.Generic;
using UnityEngine;

public class WagonModel
{
    public WagonConfig Config { get; }
    public int Hp { get; private set; }
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

    public WagonModel(WagonConfig config)
    {
        Config = config;
        Hp = config.maxHp;
        Wood = config.wood;
        Scrap = config.scrap;
        grid = new MachineModel[config.size.x, config.size.y];
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
        
        for (int i = machines.Count - 1; i >= 0; i--)
        {
            if (machines[i].IsUsedUp) RemoveMachine(machines[i]);
        }
    }
    
    private void RemoveMachine(MachineModel machine)
    {
        for (int x = 0; x < machine.Config.size.x; x++)
        for (int y = 0; y < machine.Config.size.y; y++)
            grid[machine.Cell.x + x, machine.Cell.y + y] = null;

        machines.Remove(machine);
        MachineRemoved?.Invoke(machine);
    }
    
    public bool CanPlace(MachineConfig machine, Vector2Int cell)
    {
        if (IsDestroyed) return false;

        for (int x = 0; x < machine.size.x; x++)
        for (int y = 0; y < machine.size.y; y++)
        {
            var c = new Vector2Int(cell.x + x, cell.y + y);
            if (!IsInside(c)) return false; 
            if (grid[c.x, c.y] != null) return false; 
        }
        return true;
    }

    public bool TryPlace(MachineConfig machine, Vector2Int cell)
    {
        if (!CanPlace(machine, cell)) return false;

        var placed = new MachineModel(machine, cell);
        for (int x = 0; x < machine.size.x; x++)
        for (int y = 0; y < machine.size.y; y++)
            grid[cell.x + x, cell.y + y] = placed;

        machines.Add(placed);
        MachinePlaced?.Invoke(placed);
        return true;
    }
    
    public MachineModel GetMachineAt(Vector2Int cell) => IsInside(cell) ? grid[cell.x, cell.y] : null;

    private bool IsInside(Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < Config.size.x && c.y < Config.size.y;
}
