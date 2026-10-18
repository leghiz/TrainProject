using System;
using System.Collections.Generic;
using UnityEngine;

public class WagonPresenter : IDisposable
{
    private readonly WagonModel model;
    private readonly WagonView view;
    private readonly BuildModel build;
    private readonly Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)> machines =
        new Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)>();

    public WagonPresenter(WagonModel model, WagonView view, BuildModel build)
    {
        this.model = model;
        this.view = view;
        this.build = build;

        view.SetSprite(model.Config.sprite);
        if (model.Condition != null) view.SetTint(model.Condition.tint);
        view.SetGridSize(model.Config.size);
        UpdateHp(model.Hp);
        foreach (var machine in model.Machines) ShowMachine(machine);

        model.HpChanged += UpdateHp;
        model.Destroyed += OnDestroyed;
        model.MachinePlaced += ShowMachine;
        model.MachineRemoved += HideMachine;

        view.CellHovered += UpdatePreview;
        view.HoverExited += view.HidePlacementPreview;
        view.CellClicked += OnCellClicked;
        build.SelectedChanged += OnSelectedChanged;
    }

    private void UpdateHp(int hp) => view.SetHp((float)hp / model.Config.maxHp);

    private void OnDestroyed() => view.ShowDestroyed();
    
    private void ShowMachine(MachineModel machine)
    {
        var machineView = view.AddMachine(machine.Cell, machine.Config.size);
        machines[machine] = (new MachinePresenter(machine, machineView), machineView);
    }
    private void HideMachine(MachineModel machine)
    {
        if (!machines.TryGetValue(machine, out var entry)) return;
        entry.presenter.Dispose();
        view.RemoveMachine(entry.view);
        machines.Remove(machine);
        if (view.HoveredCell.HasValue) UpdatePreview(view.HoveredCell.Value);
    }
    
    private void UpdatePreview(Vector2Int cell)
    {
        var machine = build.Selected;
        if (machine == null)
        {
            view.HidePlacementPreview();
            return;
        }
        view.ShowPlacementPreview(cell, machine.size, model.CanPlace(machine, cell));
    }

    private void OnCellClicked(Vector2Int cell)
    {
        var machine = build.Selected;
        if (machine == null) return;
        if (model.TryPlace(machine, cell)) UpdatePreview(cell);
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        if (view.HoveredCell.HasValue) UpdatePreview(view.HoveredCell.Value);
    }

    public void Dispose()
    {
        model.HpChanged -= UpdateHp;
        model.Destroyed -= OnDestroyed;
        model.MachinePlaced -= ShowMachine;
        model.MachineRemoved -= HideMachine;

        view.CellHovered -= UpdatePreview;
        view.HoverExited -= view.HidePlacementPreview;
        view.CellClicked -= OnCellClicked;
        build.SelectedChanged -= OnSelectedChanged;

        foreach (var entry in machines.Values) entry.presenter.Dispose();
        machines.Clear();
    }
}
