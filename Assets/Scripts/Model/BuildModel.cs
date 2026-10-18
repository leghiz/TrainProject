using System;

public class BuildModel
{
    public MachineConfig Selected { get; private set; } 

    public event Action<MachineConfig> SelectedChanged;

    public void Select(MachineConfig machine)
    {
        Selected = machine;
        SelectedChanged?.Invoke(machine);
    }
}
