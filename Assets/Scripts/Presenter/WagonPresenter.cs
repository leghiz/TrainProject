using System;
using System.Collections.Generic;
using UnityEngine;

public class WagonPresenter : IDisposable
{
    private readonly WagonModel model;
    private readonly WagonView view;
    private readonly BuildModel build;
    private readonly ResourceStorageModel storage;
    private readonly HoverModel hover;
    private readonly Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)> machines =
        new Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)>();
    private readonly Dictionary<CargoBoxModel, CargoBoxView> boxes = new Dictionary<CargoBoxModel, CargoBoxView>();

    public WagonPresenter(WagonModel model, WagonView view, BuildModel build, ResourceStorageModel storage, HoverModel hover)
    {
        this.model = model;
        this.view = view;
        this.build = build;
        this.storage = storage;
        this.hover = hover;

        view.SetSprite(model.Config.sprite);
        view.SetGridSize(model.Config.size);
        UpdateHp(model.Hp);
        foreach (var machine in model.Machines) ShowMachine(machine);
        foreach (var box in model.Boxes) ShowBox(box);

        model.HpChanged += UpdateHp;
        model.Destroyed += OnDestroyed;
        model.MachinePlaced += ShowMachine;
        model.MachineRemoved += HideMachine;
        model.BoxPlaced += ShowBox;
        model.BoxRemoved += HideBox;

        view.CellHovered += UpdatePreview;
        view.CellHovered += UpdateHover;
        view.HoverExited += view.HidePlacementPreview;
        view.HoverExited += ClearHover;
        view.CellReleased += OnCellReleased;
        build.SelectedChanged += OnSelectedChanged;
    }

    private void UpdateHp(int hp) => view.SetHp((float)hp / model.Config.maxHp);

    private void OnDestroyed() => ClearHover();

    private void UpdateHover(Vector2Int cell)
    {
        Vector3 anchor = view.TooltipPoint;
        var machine = model.GetMachineAt(cell);
        var box = model.GetBoxAt(cell);
        if (machine != null) hover.Show(this, () => TooltipTexts.Machine(machine), anchor);
        else if (box != null) hover.Show(this, () => TooltipTexts.Box(box), anchor);
        else hover.Show(this, () => TooltipTexts.Wagon(model), anchor);
    }

    private void ClearHover() => hover.Clear(this);
    
    private void ShowMachine(MachineModel machine)
    {
        var machineView = view.AddMachine(machine.Cell, machine.Config.size);
        machines[machine] = (new MachinePresenter(machine, machineView), machineView);
        if (view.HoveredCell.HasValue) UpdateHover(view.HoveredCell.Value);
    }
    private void HideMachine(MachineModel machine)
    {
        if (!machines.TryGetValue(machine, out var entry)) return;
        entry.presenter.Dispose();
        view.RemoveMachine(entry.view);
        machines.Remove(machine);
        if (view.HoveredCell.HasValue) RefreshHovered(view.HoveredCell.Value);
    }

    private void RefreshHovered(Vector2Int cell)
    {
        UpdatePreview(cell);
        UpdateHover(cell);
    }

    private void ShowBox(CargoBoxModel box) =>
        boxes[box] = view.AddBox(box.Cell, box.Config.size, box.Config.sprite);

    private void HideBox(CargoBoxModel box)
    {
        if (!boxes.TryGetValue(box, out var boxView)) return;
        view.RemoveBox(boxView);
        boxes.Remove(box);
        if (view.HoveredCell.HasValue) RefreshHovered(view.HoveredCell.Value);
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

    private void OnCellReleased(Vector2Int cell)
    {
        var machine = build.Selected;
        if (machine == null)
        {
            LoadMachineAt(cell);
            return;
        }
        if (model.TryPlace(machine, cell, build.SelectedTrait)) build.Select(null);
    }

    private void LoadMachineAt(Vector2Int cell)
    {
        var placed = model.GetMachineAt(cell);
        if (placed != null && placed.NeedsLoading) placed.Load(storage, int.MaxValue);
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
        model.BoxPlaced -= ShowBox;
        model.BoxRemoved -= HideBox;

        view.CellHovered -= UpdatePreview;
        view.CellHovered -= UpdateHover;
        view.HoverExited -= view.HidePlacementPreview;
        view.HoverExited -= ClearHover;
        view.CellReleased -= OnCellReleased;
        build.SelectedChanged -= OnSelectedChanged;
        ClearHover();

        foreach (var entry in machines.Values) entry.presenter.Dispose();
        machines.Clear();
    }
}
