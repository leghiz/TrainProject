using System;

public class BuildModel
{
    public MachineConfig Selected { get; private set; }
    public MachineTrait SelectedTrait { get; private set; }

    public event Action<MachineConfig> SelectedChanged;

    public void Select(MachineConfig machine, MachineTrait trait = null)
    {
        Selected = machine;
        SelectedTrait = machine != null ? trait : null;
        SelectedChanged?.Invoke(machine);
    }
}
