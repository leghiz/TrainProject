using System;
using UnityEngine;

public class CargoBoxModel
{
    public CargoBoxConfig Config { get; }
    public Vector2Int Cell { get; }

    public int Hp { get; private set; }
    public bool IsBroken => Hp <= 0;
    public event Action<int> HpChanged;

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
}
