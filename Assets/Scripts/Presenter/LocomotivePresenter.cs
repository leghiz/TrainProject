using System;
using System.Collections.Generic;
using UnityEngine;

public class LocomotivePresenter : IDisposable
{
    private readonly LocomotiveModel model;
    private readonly LocomotiveView view;
    private readonly BuildModel build;
    private readonly ResourceStorageModel storage;

    private readonly Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)> machines =
        new Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)>();
    private readonly Dictionary<PlateModel, MachineView> plates = new Dictionary<PlateModel, MachineView>();

    public LocomotivePresenter(LocomotiveModel model, LocomotiveView view, BuildModel build, ResourceStorageModel storage)
    {
        this.model = model;
        this.view = view;
        this.build = build;
        this.storage = storage;

        view.SetSprite(model.Config.sprite);
        if (model.Plating.SlotCount != view.PlateSlotCount)
            Debug.LogWarning($"[Обшивка] в Locomotive.asset maxPlates = {model.Plating.SlotCount}, а точек в префабе LocomotiveView — {view.PlateSlotCount}");
        RefreshHp();
        if (model.Boiler != null) ShowMachine(model.Boiler, LocomotiveSlot.Boiler);
        if (model.Workbench != null) ShowMachine(model.Workbench, LocomotiveSlot.Workbench);
        if (model.Welder != null) ShowMachine(model.Welder, LocomotiveSlot.Welder);
        foreach (var plate in model.Plating.Plates) ShowPlate(plate);

        model.HpChanged += OnHpChanged;
        model.Destroyed += OnDestroyed;
        model.MachinePlaced += OnWelderPlaced;
        model.MachineRemoved += HideMachine;
        model.Plating.PlateAdded += ShowPlate;
        model.Plating.PlateRemoved += HidePlate;
        model.Plating.Changed += RefreshHp;

        view.WelderSlotHovered += UpdatePreview;
        view.WelderSlotExited += view.HidePlacementPreview;
        view.WelderSlotReleased += OnWelderSlotReleased;
        view.PlateSlotHovered += UpdatePlatePreview;
        view.PlateSlotExited += view.HidePlacementPreview;
        view.PlateSlotReleased += OnPlateSlotReleased;
        view.BoilerClicked += OnBoilerClicked;
        build.SelectedChanged += OnSelectedChanged;
    }
    
    private void RefreshHp()
    {
        var plating = model.Plating;
        view.SetHp(plating.HasPlates
            ? (float)plating.TotalHp / plating.TotalMaxHp
            : (float)model.Hp / model.Config.maxHp);
    }

    private void OnHpChanged(int hp) => RefreshHp();

    private void ShowPlate(PlateModel plate)
    {
        if (plate.Slot >= view.PlateSlotCount) return;
        var plateView = view.AddPlate(plate.Slot);
        plateView.SetSprite(plate.Config.sprite);
        plates[plate] = plateView;
    }

    private void HidePlate(PlateModel plate)
    {
        if (!plates.TryGetValue(plate, out var plateView)) return;
        view.RemoveMachine(plateView);
        plates.Remove(plate);
        if (view.HoveredPlateSlot.HasValue) UpdatePlatePreview(view.HoveredPlateSlot.Value);
    }

    private void UpdatePlatePreview(int slot)
    {
        var machine = build.Selected;
        if (machine == null)
        {
            view.HidePlacementPreview();
            return;
        }
        view.ShowPlatePreview(slot, model.CanPlacePlate(machine, slot));
    }

    private void OnPlateSlotReleased(int slot)
    {
        var machine = build.Selected;
        if (machine == null) return;
        if (model.TryPlacePlate(machine, slot)) build.Select(null);
    }
    
    private void OnBoilerClicked()
    {
        if (build.Selected != null || model.Boiler == null) return;
        model.Boiler.Load(storage, int.MaxValue);
    }

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

    private void OnWelderSlotReleased()
    {
        var machine = build.Selected;
        if (machine == null) return;
        if (model.TryPlaceWelder(machine, build.SelectedTrait)) build.Select(null);
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        if (view.IsWelderSlotHovered) UpdatePreview();
        if (view.HoveredPlateSlot.HasValue) UpdatePlatePreview(view.HoveredPlateSlot.Value);
    }

    public void Dispose()
    {
        model.HpChanged -= OnHpChanged;
        model.Destroyed -= OnDestroyed;
        model.MachinePlaced -= OnWelderPlaced;
        model.MachineRemoved -= HideMachine;
        model.Plating.PlateAdded -= ShowPlate;
        model.Plating.PlateRemoved -= HidePlate;
        model.Plating.Changed -= RefreshHp;

        view.WelderSlotHovered -= UpdatePreview;
        view.WelderSlotExited -= view.HidePlacementPreview;
        view.WelderSlotReleased -= OnWelderSlotReleased;
        view.PlateSlotHovered -= UpdatePlatePreview;
        view.PlateSlotExited -= view.HidePlacementPreview;
        view.PlateSlotReleased -= OnPlateSlotReleased;
        view.BoilerClicked -= OnBoilerClicked;
        build.SelectedChanged -= OnSelectedChanged;

        foreach (var entry in machines.Values) entry.presenter.Dispose();
        machines.Clear();
        plates.Clear();
    }
}
