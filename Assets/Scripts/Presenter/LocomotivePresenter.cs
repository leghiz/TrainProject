using System;
using System.Collections.Generic;

public class LocomotivePresenter : IDisposable
{
    private readonly LocomotiveModel model;
    private readonly LocomotiveView view;
    private readonly BuildModel build;

    private readonly Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)> machines =
        new Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)>();

    public LocomotivePresenter(LocomotiveModel model, LocomotiveView view, BuildModel build)
    {
        this.model = model;
        this.view = view;
        this.build = build;

        view.SetSprite(model.Config.sprite);
        UpdateHp(model.Hp);
        if (model.Boiler != null) ShowMachine(model.Boiler, LocomotiveSlot.Boiler);
        if (model.Workbench != null) ShowMachine(model.Workbench, LocomotiveSlot.Workbench);
        if (model.Welder != null) ShowMachine(model.Welder, LocomotiveSlot.Welder);

        model.HpChanged += UpdateHp;
        model.Destroyed += OnDestroyed;
        model.MachinePlaced += OnWelderPlaced;
        model.MachineRemoved += HideMachine;

        view.WelderSlotHovered += UpdatePreview;
        view.WelderSlotExited += view.HidePlacementPreview;
        view.WelderSlotClicked += OnWelderSlotClicked;
        build.SelectedChanged += OnSelectedChanged;
    }

    private void UpdateHp(int hp) => view.SetHp((float)hp / model.Config.maxHp);

    private void OnDestroyed() => view.ShowDestroyed();

    private void ShowMachine(MachineModel machine, LocomotiveSlot slot)
    {
        var machineView = view.AddMachine(slot);
        machines[machine] = (new MachinePresenter(machine, machineView), machineView);
    }

    private void OnWelderPlaced(MachineModel machine) => ShowMachine(machine, LocomotiveSlot.Welder);

    private void HideMachine(MachineModel machine)
    {
        if (!machines.TryGetValue(machine, out var entry)) return;
        entry.presenter.Dispose();
        view.RemoveMachine(entry.view);
        machines.Remove(machine);
        if (view.IsWelderSlotHovered) UpdatePreview();
    }

    private void UpdatePreview()
    {
        var machine = build.Selected;
        if (machine == null)
        {
            view.HidePlacementPreview();
            return;
        }
        view.ShowWelderPreview(model.CanPlaceWelder(machine));
    }

    private void OnWelderSlotClicked()
    {
        var machine = build.Selected;
        if (machine == null) return;
        if (model.TryPlaceWelder(machine)) UpdatePreview();
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        if (view.IsWelderSlotHovered) UpdatePreview();
    }

    public void Dispose()
    {
        model.HpChanged -= UpdateHp;
        model.Destroyed -= OnDestroyed;
        model.MachinePlaced -= OnWelderPlaced;
        model.MachineRemoved -= HideMachine;

        view.WelderSlotHovered -= UpdatePreview;
        view.WelderSlotExited -= view.HidePlacementPreview;
        view.WelderSlotClicked -= OnWelderSlotClicked;
        build.SelectedChanged -= OnSelectedChanged;

        foreach (var entry in machines.Values) entry.presenter.Dispose();
        machines.Clear();
    }
}
