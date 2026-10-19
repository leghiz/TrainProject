using System;

public class BuildModel
{
    public MachineConfig Selected { get; private set; }
    public MachineTrait SelectedTrait { get; private set; }

    // Станок, который сняли с вагона для перестановки (пусто — в руке новый станок).
    public MachineModel Moving { get; private set; }

    public event Action<MachineConfig> SelectedChanged;

    public event Action<MachineConfig, MachineTrait> Cancelled;

    public void Select(MachineConfig machine, MachineTrait trait = null) => Set(machine, trait, null);

    public void SelectMoving(MachineModel machine) => Set(machine.Config, machine.Trait, machine);

    public void Cancel()
    {
        if (Selected == null) return;
        Cancelled?.Invoke(Selected, SelectedTrait);
        Select(null);
    }

    private void Set(MachineConfig machine, MachineTrait trait, MachineModel moving)
    {
        Selected = machine;
        SelectedTrait = machine != null ? trait : null;
        Moving = machine != null ? moving : null;
        SelectedChanged?.Invoke(machine);
    }
}
