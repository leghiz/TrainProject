using System;
using UnityEngine;

public class CargoBoxModel
{
    public CargoBoxConfig Config { get; }
    public Vector2Int Cell { get; private set; }

    public int Hp { get; private set; }
    public bool IsBroken => Hp <= 0;
    public event Action<int> HpChanged;

    // Ящик переставили (в тот же вагон или в другой) — доставка следит, в каком он вагоне.
    public event Action<WagonModel> Moved;

    public CargoBoxModel(CargoBoxConfig config, Vector2Int cell)
    {
        Config = config;
        Cell = cell;
        Hp = config.maxHp;
    }

    public void Damage(int amount)
    {
        if (IsBroken || amount <= 0) return;
        Hp = Mathf.Max(0, Hp - amount);
        HpChanged?.Invoke(Hp);
    }

    public void MoveTo(Vector2Int cell, WagonModel wagon)
    {
        Cell = cell;
        Moved?.Invoke(wagon);
    }
}
