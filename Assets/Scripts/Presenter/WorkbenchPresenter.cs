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

    private readonly List<(MachineConfig recipe, RecipeRowView row, Action onClick)> rows =
        new List<(MachineConfig, RecipeRowView, Action)>();

    private MachineConfig taken;

    public WorkbenchPresenter(WorkbenchModel model, ResourceStorageModel storage, BuildModel build,
                              LocomotiveView locomotiveView, WorkbenchPanelView panel)
    {
        this.model = model;
        this.storage = storage;
        this.build = build;
        this.locomotiveView = locomotiveView;
        this.panel = panel;

        BindRows();
        panel.Hide();
        UpdateReady(model.Ready);
        RefreshRows();

        model.ReadyChanged += OnReadyChanged;
        storage.Changed += RefreshRows;
        panel.CloseClicked += panel.Hide;
        locomotiveView.WorkbenchClicked += OnWorkbenchClicked;
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
        if (panel.IsOpen) panel.Hide();
        else
        {
            RefreshRows();
            panel.Show();
        }
    }
    
    private void OnCraftClicked(MachineConfig recipe)
    {
        if (model.TryCraft(recipe)) panel.Hide();
    }

    private void OnReadyChanged(MachineConfig machine)
    {
        UpdateReady(machine);
        RefreshRows();
    }

    private void UpdateReady(MachineConfig machine)
    {
        if (machine != null) locomotiveView.ShowReady(machine.sprite);
        else locomotiveView.HideReady();
    }

    private void RefreshRows()
    {
        foreach (var entry in rows) entry.row.SetInteractable(model.CanCraft(entry.recipe));
    }

    private void OnReadyIconPressed()
    {
        if (build.Selected != null || !model.HasReady) return;
        var machine = model.Take();
        build.Select(machine);
        taken = machine;
    }

    private void OnCancelled(MachineConfig machine, MachineTrait trait)
    {
        if (taken == null || machine != taken) return;
        taken = null;
        model.PutBack(machine);
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        if (machine != taken) taken = null;
    }

    public void Dispose()
    {
        model.ReadyChanged -= OnReadyChanged;
        storage.Changed -= RefreshRows;
        panel.CloseClicked -= panel.Hide;
        locomotiveView.WorkbenchClicked -= OnWorkbenchClicked;
        locomotiveView.ReadyIconPressed -= OnReadyIconPressed;
        build.SelectedChanged -= OnSelectedChanged;
        build.Cancelled -= OnCancelled;

        foreach (var entry in rows) entry.row.Clicked -= entry.onClick;
        rows.Clear();
    }
}
