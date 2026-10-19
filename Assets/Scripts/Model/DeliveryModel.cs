using System;
using System.Collections.Generic;

public class DeliveryModel
{
    public CargoBoxModel Box { get; private set; }
    public WagonModel Wagon { get; private set; }
    public int DestinationColumn { get; private set; }
    public bool IsActive => Box != null;

    public event Action Started;
    public event Action<int> Delivered;
    public event Action<MachineConfig, MachineTrait> Disassembled;
    public event Action<string> Failed;

    public void Start(CargoBoxModel box, WagonModel wagon, int destinationColumn)
    {
        Box = box;
        Wagon = wagon;
        DestinationColumn = destinationColumn;
        wagon.BoxRemoved += OnBoxRemoved;
        wagon.Destroyed += OnWagonDestroyed;
        Started?.Invoke();
    }
    
    public bool TryDeliver(MapNode node, StationConfig deliveryStation, ResourceStorageModel storage)
    {
        if (!IsActive || node.Column != DestinationColumn || node.Station != deliveryStation) return false;

        int reward = Box.Config.rewardIngots;
        RemoveBoxQuietly();
        storage.Add(ResourceType.Ingot, reward);
        Delivered?.Invoke(reward);
        return true;
    }
    
    public void CheckMissed(MapNode node)
    {
        if (IsActive && node.Column >= DestinationColumn) Fail("не довезли до Грузовой");
    }
    
    public bool Disassemble(IList<MachineConfig> machines, IList<MachineTrait> traits, Random random,
                            out MachineConfig machine, out MachineTrait trait)
    {
        machine = null;
        trait = null;
        if (!IsActive || machines == null || machines.Count == 0) return false;

        machine = machines[random.Next(machines.Count)];
        if (traits != null && traits.Count > 0) trait = traits[random.Next(traits.Count)];
        RemoveBoxQuietly();
        Disassembled?.Invoke(machine, trait);
        return true;
    }

    private void OnBoxRemoved(CargoBoxModel box)
    {
        if (box == Box) Fail("ящик разрушен");
    }

    private void OnWagonDestroyed() => Fail("вагон с ящиком отвалился");
    
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
        if (Wagon != null)
        {
            Wagon.BoxRemoved -= OnBoxRemoved;
            Wagon.Destroyed -= OnWagonDestroyed;
        }
        Box = null;
        Wagon = null;
    }
}
