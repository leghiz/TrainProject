using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkbenchPresenter : IDisposable
{
    private readonly WorkbenchModel model;
    private readonly ResourceStorageModel storage;
    private readonly BuildModel build;
    private readonly LocomotiveView locomotiveView;
    private readonly WorkbenchPanelView panel;
    private readonly HoverModel hover;

    private readonly List<(MachineConfig recipe, RecipeRowView row, Action onClick)> rows =
        new List<(MachineConfig, RecipeRowView, Action)>();

    // Наведение на станок в меню — свой тултип (отдельный владелец, чтобы не путать с тултипом самого верстака).
    private readonly object menuHover = new object();
    private readonly List<(RecipeRowView row, Action onHover)> rowHovers = new List<(RecipeRowView, Action)>();

    private MachineConfig taken;

    public WorkbenchPresenter(WorkbenchModel model, ResourceStorageModel storage, BuildModel build,
                              LocomotiveView locomotiveView, WorkbenchPanelView panel, HoverModel hover)
    {
        this.model = model;
        this.storage = storage;
        this.build = build;
        this.locomotiveView = locomotiveView;
        this.panel = panel;
        this.hover = hover;

        BindRows();
        panel.Hide();
        UpdateReady(model.Ready);
        OnProgressChanged();

        model.ReadyChanged += OnReadyChanged;
        model.ProgressChanged += OnProgressChanged;
        locomotiveView.PartHovered += OnPartHovered;
        storage.Changed += RefreshRows;
        panel.CloseClicked += HidePanel;
        locomotiveView.WorkbenchClicked += OnWorkbenchClicked;
        locomotiveView.BoilerClicked += HidePanel;
        locomotiveView.FoggedChanged += OnFogged;
        locomotiveView.ReadyIconPressed += OnReadyIconPressed;
        build.SelectedChanged += OnSelectedChanged;
        build.Cancelled += OnCancelled;
    }
    
    private void BindRows()
    {
        var sceneRows = panel.Rows;
        foreach (var row in sceneRows)
        {
            if (row.Recipe == null)
            {
                Debug.LogWarning($"[Верстак] у строки «{row.name}» в меню не выбран станок (поле Recipe)");
                row.SetInteractable(false);
                continue;
            }
            if (!model.IsRecipe(row.Recipe))
            {
                Debug.LogWarning($"[Верстак] строка «{row.name}»: станка {row.Recipe.displayName} нет в recipes у Locomotive.asset — кнопка выключена");
                row.SetInteractable(false);
                continue;
            }
            row.Refresh();
            var recipe = row.Recipe;
            Action onClick = () => OnCraftClicked(recipe);
            row.Clicked += onClick;
            rows.Add((recipe, row, onClick));

            var hoveredRow = row;
            Action onHover = () => hover.Show(menuHover, () => MenuTooltip(recipe, hoveredRow), hoveredRow.IconCenter);
            row.Hovered += onHover;
            row.Unhovered += ClearMenuHover;
            rowHovers.Add((row, onHover));
        }

        foreach (var recipe in model.Recipes)
        {
            bool found = false;
            foreach (var entry in rows) if (entry.recipe == recipe) found = true;
            if (!found) Debug.LogWarning($"[Верстак] для станка {recipe.displayName} нет строки в меню (Canvas → WorkbenchPanel)");
        }
    }

    private void OnWorkbenchClicked()
    {
        if (panel.IsOpen) HidePanel();
        else
        {
            RefreshRows();
            panel.Show();
            UpdateReady(model.Ready);
            UpdateProgress();
            locomotiveView.HideHp(this, true);
            locomotiveView.HighlightWorkbench(true);
        }
    }

    // Пока меню открыто, готовый станок над локомотивом не показываем — он лёг бы поверх меню.
    private void HidePanel()
    {
        panel.Hide();
        UpdateReady(model.Ready);
        UpdateProgress();
        locomotiveView.HideHp(this, false);
        locomotiveView.HighlightWorkbench(false);
    }

    // В тумане камера ушла к другому вагону — меню верстака закрывается.
    private void OnFogged(bool fogged)
    {
        if (fogged && panel.IsOpen) HidePanel();
    }

    private void OnCraftClicked(MachineConfig recipe)
    {
        if (model.TryCraft(recipe)) HidePanel();
    }

    private void OnReadyChanged(MachineConfig machine)
    {
        if (machine == null) hover.Clear(this);
        UpdateReady(machine);
        RefreshRows();
    }

    private void UpdateReady(MachineConfig machine)
    {
        if (machine != null && !panel.IsOpen) locomotiveView.ShowReady(machine.sprite);
        else locomotiveView.HideReady();
    }

    private void OnProgressChanged()
    {
        UpdateProgress();
        RefreshRows();
    }

    // Пока меню открыто, полоску крафта над верстаком не показываем — она легла бы на меню.
    private void UpdateProgress() =>
        locomotiveView.SetCraftProgress(model.IsCrafting && !panel.IsOpen ? model.Progress : (float?)null);
    
    private void OnPartHovered(LocomotivePart part)
    {
        if (part == LocomotivePart.Ready && model.HasReady)
            hover.Show(this, () => TooltipTexts.ReadyMachine(model.Ready, model.ReadyTrait), locomotiveView.ReadyIconCenter);
        else hover.Clear(this);
    }

    private void RefreshRows()
    {
        foreach (var entry in rows) entry.row.SetInteractable(model.CanCraft(entry.recipe));
    }

    private void OnReadyIconPressed()
    {
        if (!build.IsEmpty || !model.HasReady) return;
        var machine = model.Take(out var trait);
        build.Select(machine, trait);
        taken = machine;
    }

    private void OnCancelled(MachineConfig machine, MachineTrait trait)
    {
        if (taken == null || machine != taken) return;
        taken = null;
        model.PutBack(machine, trait);
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        if (machine != taken) taken = null;
    }

    public void Dispose()
    {
        model.ReadyChanged -= OnReadyChanged;
        model.ProgressChanged -= OnProgressChanged;
        locomotiveView.PartHovered -= OnPartHovered;
        hover.Clear(this);
        storage.Changed -= RefreshRows;
        panel.CloseClicked -= HidePanel;
        locomotiveView.WorkbenchClicked -= OnWorkbenchClicked;
        locomotiveView.BoilerClicked -= HidePanel;
        locomotiveView.FoggedChanged -= OnFogged;
        locomotiveView.ReadyIconPressed -= OnReadyIconPressed;
        build.SelectedChanged -= OnSelectedChanged;
        build.Cancelled -= OnCancelled;

        foreach (var entry in rows) entry.row.Clicked -= entry.onClick;
        rows.Clear();
        foreach (var entry in rowHovers)
        {
            entry.row.Hovered -= entry.onHover;
            entry.row.Unhovered -= ClearMenuHover;
        }
        rowHovers.Clear();
        ClearMenuHover();
    }

    private void ClearMenuHover() => hover.Clear(menuHover);

    // В меню тултип станка встаёт справа от него, чтобы не налезать на соседние строки.
    private static TooltipContent MenuTooltip(MachineConfig recipe, RecipeRowView row)
    {
        var content = TooltipTexts.ReadyMachine(recipe, null);
        content.Beside = true;
        content.Size = row.IconSize;
        return content;
    }
}
