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
    private readonly CargoSelectionModel cargoSelection;
    private readonly DismantleModel dismantle;
    private object dismantleTarget;
    private (MachineModel machine, Vector2Int cell) movingFrom;
    private readonly Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)> machines =
        new Dictionary<MachineModel, (MachinePresenter presenter, MachineView view)>();
    private readonly Dictionary<CargoBoxModel, CargoBoxView> boxes = new Dictionary<CargoBoxModel, CargoBoxView>();

    public WagonPresenter(WagonModel model, WagonView view, BuildModel build, ResourceStorageModel storage,
                          HoverModel hover, CargoSelectionModel cargoSelection, DismantleModel dismantle)
    {
        this.model = model;
        this.view = view;
        this.build = build;
        this.storage = storage;
        this.hover = hover;
        this.cargoSelection = cargoSelection;
        this.dismantle = dismantle;

        view.SetSprite(model.Config.sprite);
        if (model.Condition != null) view.SetMaterial(model.Condition.material);
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
        view.CellRightPressed += OnCellRightPressed;
        view.CellPressed += OnCellPressed;
        view.CellHovered += CancelDismantleIfMovedAway;
        view.HoverExited += CancelDismantle;
        build.SelectedChanged += OnSelectedChanged;
        build.Cancelled += OnCancelled;
        RefreshGrid(build.Selected);
    }

    // Удержание ЛКМ над станком с пустой рукой — взять его в руку и переставить.
    private void OnCellPressed(Vector2Int cell)
    {
        if (build.Selected != null) return;
        var machine = model.GetMachineAt(cell);
        if (machine == null) return;
        dismantleTarget = machine;
        dismantle.Begin(this, () => PickUp(machine));
    }

    private void PickUp(MachineModel machine)
    {
        if (!model.TakeMachine(machine)) return;
        ClearHover();
        movingFrom = (machine, machine.Cell);
        build.SelectMoving(machine);
    }

    // Отпустили станок мимо или нажали ПКМ — он возвращается на своё место.
    private void OnCancelled(MachineConfig machine, MachineTrait trait)
    {
        if (movingFrom.machine == null || build.Moving != movingFrom.machine) return;
        model.TryPlaceExisting(movingFrom.machine, movingFrom.cell);
        movingFrom = default;
    }

    // Удержание ПКМ над станком или ящиком с пустой рукой — разборка (ПКМ с предметом в руке — это отмена).
    private void OnCellRightPressed(Vector2Int cell)
    {
        if (build.Selected != null) return;
        var machine = model.GetMachineAt(cell);
        var box = model.GetBoxAt(cell);
        if (machine != null)
        {
            dismantleTarget = machine;
            dismantle.Begin(this, () => model.Dismantle(machine, storage));
        }
        else if (box != null)
        {
            dismantleTarget = box;
            dismantle.Begin(this, () => cargoSelection.Select(box, view.AreaTop(box.Cell, box.Config.size)));
        }
    }

    private void CancelDismantleIfMovedAway(Vector2Int cell)
    {
        object under = (object)model.GetMachineAt(cell) ?? model.GetBoxAt(cell);
        if (under != dismantleTarget) CancelDismantle();
    }

    private void CancelDismantle()
    {
        dismantleTarget = null;
        dismantle.Cancel(this);
    }

    private void UpdateHp(int hp) => view.SetHp((float)hp / model.Config.maxHp);

    private void OnDestroyed()
    {
        ClearHover();
        CancelDismantle();
    }

    private void UpdateHover(Vector2Int cell)
    {
        Vector3 anchor = view.TooltipPoint;
        var machine = model.GetMachineAt(cell);
        var box = model.GetBoxAt(cell);
        if (machine != null)
        {
            Vector3 machinePoint = machines.TryGetValue(machine, out var entry) ? entry.view.transform.position : anchor;
            hover.Show(this, () => TooltipTexts.Machine(machine), machinePoint);
        }
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
        if (build.Moving != null)
        {
            if (model.TryPlaceExisting(build.Moving, cell)) build.Select(null);
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
        if (build.Moving == null) movingFrom = default;
        RefreshGrid(machine);
        if (view.HoveredCell.HasValue) UpdatePreview(view.HoveredCell.Value);
    }

    // Пока станок в руке — на вагоне видна сетка свободных клеток.
    private void RefreshGrid(MachineConfig machine)
    {
        if (machine == null || machine.action == MachineAction.Plating)
        {
            view.HideGrid();
            return;
        }

        var free = new List<Vector2Int>();
        for (int x = 0; x < model.Config.size.x; x++)
            for (int y = 0; y < model.Config.size.y; y++)
            {
                var cell = new Vector2Int(x, y);
                if (model.GetMachineAt(cell) == null && model.GetBoxAt(cell) == null) free.Add(cell);
            }
        view.ShowGrid(free);
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
        view.CellRightPressed -= OnCellRightPressed;
        view.CellPressed -= OnCellPressed;
        build.Cancelled -= OnCancelled;
        view.CellHovered -= CancelDismantleIfMovedAway;
        view.HoverExited -= CancelDismantle;
        build.SelectedChanged -= OnSelectedChanged;
        ClearHover();
        CancelDismantle();

        foreach (var entry in machines.Values) entry.presenter.Dispose();
        machines.Clear();
    }
}
