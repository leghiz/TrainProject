using System;
using System.Collections.Generic;
using UnityEngine;


public class TrainModel
{
    public LocomotiveModel Locomotive { get; }

    private readonly List<WagonModel> wagons = new List<WagonModel>();
    public IReadOnlyList<WagonModel> Wagons => wagons;

    public event Action<WagonModel> WagonAttached;
    public event Action<WagonModel> WagonDetached;

    private readonly Dictionary<WagonModel, Action> destroyedHandlers = new Dictionary<WagonModel, Action>();

    public TrainModel(LocomotiveModel locomotive)
    {
        Locomotive = locomotive;
    }

    public void Attach(WagonModel wagon)
    {
        if (wagon == null || wagon.IsDestroyed || wagons.Contains(wagon)) return;

        wagons.Add(wagon);
        Action handler = () => Detach(wagon);
        destroyedHandlers[wagon] = handler;
        wagon.Destroyed += handler;
        WagonAttached?.Invoke(wagon);
    }

    public void Detach(WagonModel wagon)
    {
        if (!wagons.Remove(wagon)) return;

        if (destroyedHandlers.TryGetValue(wagon, out var handler))
        {
            wagon.Destroyed -= handler;
            destroyedHandlers.Remove(wagon);
        }
        WagonDetached?.Invoke(wagon);
    }

    public int Cells
    {
        get
        {
            int cells = 0;
            foreach (var wagon in wagons) cells += wagon.Config.size.x * wagon.Config.size.y;
            return cells;
        }
    }
    
    public float Speed
    {
        get
        {
            if (!Locomotive.HasLevels) return 0f;

            var level = Locomotive.Level;
            float speed = level.speed;
            int cells = Cells;
            if (cells > level.traction) speed *= (float)level.traction / cells;
            if (!Locomotive.HasFuel) speed *= Mathf.Max(0.05f, Locomotive.Config.noFuelSpeed);
            return speed;
        }
    }
    
    public void Tick(float deltaTime, ResourceStorageModel storage)
    {
        Locomotive.Tick(deltaTime, storage);
        
        for (int i = wagons.Count - 1; i >= 0; i--)
        {
            if (i < wagons.Count) wagons[i].Tick(deltaTime, storage);
        }

        Locomotive.ApplyWear(deltaTime);
    }
}
