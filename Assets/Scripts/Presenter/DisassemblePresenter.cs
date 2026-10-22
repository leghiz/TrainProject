using System;
using System.Collections.Generic;
using UnityEngine;

// Удержали ПКМ над ящиком посылки до конца → посылка провалена, а из ящика выпадают станки (какие — в конфиге ящика),
// у каждого случайное свойство. Встают на место ящика в вагоне; не влезли — в любые свободные клетки этого вагона,
// а если и там нет места — над локомотивом, как скрафченный.
public class DisassemblePresenter : IDisposable
{
    private readonly CargoSelectionModel selection;
    private readonly JourneyModel journey;
    private readonly WorkbenchModel workbench;
    private readonly List<(MachineConfig machine, MachineTrait trait)> dropped = new List<(MachineConfig, MachineTrait)>();

    public DisassemblePresenter(CargoSelectionModel selection, JourneyModel journey, WorkbenchModel workbench)
    {
        this.selection = selection;
        this.journey = journey;
        this.workbench = workbench;

        selection.Changed += OnSelected;
    }

    private void OnSelected()
    {
        var box = selection.Box;
        if (box == null) return;
        selection.Clear();
        if (box != journey.Delivery.Box) return;

        var wagon = journey.Delivery.Wagon;
        var cell = box.Cell;
        var size = box.Config.size;
        dropped.Clear();
        if (!journey.DisassembleCargo(dropped)) return;

        foreach (var item in dropped)
        {
            if (wagon != null && (PlaceIn(wagon, item, cell, size) || PlaceIn(wagon, item, Vector2Int.zero, wagon.Config.size))) continue;
            workbench.TryGive(item.machine, item.trait);
        }
    }

    private static bool PlaceIn(WagonModel wagon, (MachineConfig machine, MachineTrait trait) item, Vector2Int origin, Vector2Int size)
    {
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
                if (wagon.TryPlace(item.machine, origin + new Vector2Int(x, y), item.trait)) return true;
        return false;
    }

    public void Dispose() => selection.Changed -= OnSelected;
}
