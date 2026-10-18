using System;
using UnityEngine;

// Конкретный вагон в забеге. Создаётся по карточке WagonConfig и меняется во время игры.
// Обычный класс, не MonoBehaviour: ничего не знает про сцену и картинки.
public class WagonModel
{
    public WagonConfig Config { get; }
    public int Hp { get; private set; }
    public int Wood { get; private set; }   
    public int Scrap { get; private set; }  
    public bool IsDestroyed => Hp <= 0;

    public event Action<int> HpChanged;   
    public event Action Destroyed;        

    public WagonModel(WagonConfig config)
    {
        Config = config;
        Hp = config.maxHp;
        Wood = config.wood;
        Scrap = config.scrap;
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
}
