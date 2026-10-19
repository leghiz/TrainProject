using System;
using System.Collections.Generic;
using UnityEngine;

public class StormModel
{
    public StormConfig Config { get; }
    public int Node { get; private set; } = 1;
    
    public float Position { get; private set; }

    public event Action<float> PositionChanged;
    public event Action<int> NodeChanged;

    private float pendingDamage;

    // startPosition — где буря стоит в начале (в клетках за локомотивом). Не задано — берётся startDistance из конфига.
    public StormModel(StormConfig config, TrainModel train, float? startPosition = null)
    {
        Config = config;
        Position = startPosition ?? WagonsLength(train) + config.startDistance;
    }

    public float Speed
    {
        get
        {
            var speeds = Config.speedByNode;
            if (speeds == null || speeds.Length == 0) return 0f;
            int i = Node - 1;
            if (i < speeds.Length) return speeds[i];
            return speeds[speeds.Length - 1] + (i - (speeds.Length - 1)) * Config.speedStepAfterList;
        }
    }
    public float DistanceToTail(TrainModel train) => Position - WagonsLength(train);

    public void NextNode()
    {
        Node++;
        NodeChanged?.Invoke(Node);
    }

    public void Tick(float deltaTime, TrainModel train)
    {
        float closing = (Speed - train.Speed) * Config.cellsPerSecondAtSpeed1 * deltaTime;
        Position = Mathf.Max(Position - closing, -train.Locomotive.Config.length);
        PositionChanged?.Invoke(Position);

        DamageCovered(deltaTime, train);
    }
    
    private void DamageCovered(float deltaTime, TrainModel train)
    {
        if (Position >= WagonsLength(train))
        {
            pendingDamage = 0f;
            return;
        }

        pendingDamage += Config.damagePerSecond * deltaTime;
        int damage = Mathf.FloorToInt(pendingDamage);
        if (damage <= 0) return;
        pendingDamage -= damage;

        var covered = new List<WagonModel>();
        float wagonEnd = 0f;
        foreach (var wagon in train.Wagons)
        {
            wagonEnd += wagon.Config.size.x;
            if (wagonEnd > Position) covered.Add(wagon);
        }

        foreach (var wagon in covered) wagon.Damage(damage);
        if (Position < 0f) train.Locomotive.Damage(damage);
    }

    private static float WagonsLength(TrainModel train)
    {
        float length = 0f;
        foreach (var wagon in train.Wagons) length += wagon.Config.size.x;
        return length;
    }
}
