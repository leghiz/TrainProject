using System;
using System.Collections.Generic;

// Награда за довезённый ящик: ждёт на экране станции, пока игрок не заберёт станок и слитки.
public class DeliveryReward
{
    public CargoBoxConfig Box { get; }
    public int Ingots { get; }
    public MachineConfig Machine { get; }
    public MachineTrait Trait { get; }
    public bool IngotsTaken { get; set; }
    public bool MachineTaken { get; set; }
    public bool IsDone => IngotsTaken && (MachineTaken || Machine == null);

    public DeliveryReward(CargoBoxConfig box, int ingots, MachineConfig machine, MachineTrait trait)
    {
        Box = box;
        Ingots = ingots;
        Machine = machine;
        Trait = trait;
    }
}

public class DeliveryModel
{
    public CargoBoxModel Box { get; private set; }
    public WagonModel Wagon { get; private set; }
    public int DestinationColumn { get; private set; }
    public bool IsActive => Box != null;
    public DeliveryReward Reward { get; private set; }

    public event Action Started;
    public event Action<int> Delivered;
    public event Action<MachineConfig, MachineTrait> Disassembled;
    public event Action<string> Failed;

    public void Start(CargoBoxModel box, WagonModel wagon, int destinationColumn)
    {
        Box = box;
        DestinationColumn = destinationColumn;
        Follow(wagon);
        box.Moved += OnBoxMoved;
        Started?.Invoke();
    }
    
    // Довезли: ящик пропадает, а награда (слитки и случайный станок) ждёт игрока на экране станции.
    public bool TryDeliver(MapNode node, StationConfig deliveryStation, IList<MachineTrait> traits, Random random)
    {
        if (!IsActive || node.Column != DestinationColumn || node.Station != deliveryStation) return false;

        var config = Box.Config;
        var machines = config.deliveryMachines;
        var machine = machines != null && machines.Length > 0 ? machines[random.Next(machines.Length)] : null;
        RemoveBoxQuietly();
        Reward = new DeliveryReward(config, config.rewardIngots, machine, machine != null ? RandomTrait(traits, random) : null);
        Delivered?.Invoke(config.rewardIngots);
        return true;
    }

    public void ClearReward() => Reward = null;
    
    public void CheckMissed(MapNode node)
    {
        if (IsActive && node.Column >= DestinationColumn) Fail("не довезли до Грузовой");
    }
    
    // Разобрали ящик: выпадают все станки из его списка (если список пуст — один случайный из запасного), у каждого случайное свойство.
    public bool Disassemble(IList<MachineConfig> fallback, IList<MachineTrait> traits, Random random,
                            List<(MachineConfig machine, MachineTrait trait)> result)
    {
        if (!IsActive) return false;
        var own = Box.Config.disassemblyMachines;
        if (own != null && own.Length > 0)
        {
            foreach (var machine in own)
                if (machine != null) result.Add((machine, RandomTrait(traits, random)));
        }
        else if (fallback != null && fallback.Count > 0)
            result.Add((fallback[random.Next(fallback.Count)], RandomTrait(traits, random)));
        if (result.Count == 0) return false;

        RemoveBoxQuietly();
        foreach (var item in result) Disassembled?.Invoke(item.machine, item.trait);
        return true;
    }

    private static MachineTrait RandomTrait(IList<MachineTrait> traits, Random random) =>
        traits != null && traits.Count > 0 ? traits[random.Next(traits.Count)] : null;

    private void OnBoxRemoved(CargoBoxModel box)
    {
        if (box == Box) Fail("ящик разрушен");
    }

    private void OnWagonDestroyed() => Fail("вагон с ящиком отвалился");

    // Ящик переставили в другой вагон — теперь следим за ним.
    private void OnBoxMoved(WagonModel wagon)
    {
        if (!IsActive || wagon == Wagon) return;
        Unfollow();
        Follow(wagon);
    }

    private void Follow(WagonModel wagon)
    {
        Wagon = wagon;
        wagon.BoxRemoved += OnBoxRemoved;
        wagon.Destroyed += OnWagonDestroyed;
    }

    private void Unfollow()
    {
        if (Wagon == null) return;
        Wagon.BoxRemoved -= OnBoxRemoved;
        Wagon.Destroyed -= OnWagonDestroyed;
    }
    
    public void OnWagonDetached(WagonModel wagon)
    {
        if (IsActive && wagon == Wagon) Fail("вагон с ящиком отцепился");
    }

    private void Fail(string reason)
    {
        Clear();
        Failed?.Invoke(reason);
    }
    
    private void RemoveBoxQuietly()
    {
        var box = Box;
        var wagon = Wagon;
        Clear();
        wagon.RemoveBox(box);
    }

    private void Clear()
    {
        Unfollow();
        if (Box != null) Box.Moved -= OnBoxMoved;
        Box = null;
        Wagon = null;
    }
}
