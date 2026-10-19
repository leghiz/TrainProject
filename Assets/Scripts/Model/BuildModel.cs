using System;
using UnityEngine;

public class BuildModel
{
    public MachineConfig Selected { get; private set; }
    public MachineTrait SelectedTrait { get; private set; }

    // Станок, который сняли с вагона для перестановки (пусто — в руке новый станок).
    public MachineModel Moving { get; private set; }

    // Ящик посылки, который сняли с вагона для перестановки.
    public CargoBoxModel MovingBox { get; private set; }
    // За какую клетку ящика его взяли: она остаётся под мышкой.
    public Vector2Int BoxGrab { get; private set; }

    public bool IsEmpty => Selected == null && MovingBox == null;

    public event Action<MachineConfig> SelectedChanged;

    public event Action<MachineConfig, MachineTrait> Cancelled;
    public event Action<CargoBoxModel> BoxCancelled;

    public void Select(MachineConfig machine, MachineTrait trait = null) => Set(machine, trait, null, null, default);

    public void SelectMoving(MachineModel machine) => Set(machine.Config, machine.Trait, machine, null, default);

    public void SelectBox(CargoBoxModel box, Vector2Int grab) => Set(null, null, null, box, grab);

    public void Cancel()
    {
        if (MovingBox != null)
        {
            BoxCancelled?.Invoke(MovingBox);
            Select(null);
            return;
        }
        if (Selected == null) return;
        Cancelled?.Invoke(Selected, SelectedTrait);
        Select(null);
    }

    private void Set(MachineConfig machine, MachineTrait trait, MachineModel moving, CargoBoxModel box, Vector2Int grab)
    {
        Selected = machine;
        SelectedTrait = machine != null ? trait : null;
        Moving = machine != null ? moving : null;
        MovingBox = machine == null ? box : null;
        BoxGrab = MovingBox != null ? grab : default;
        SelectedChanged?.Invoke(machine);
    }
}
