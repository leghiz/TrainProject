using System;

// Удержали ПКМ над ящиком посылки до конца → посылка провалена,
// а из ящика выпадает станок со случайным свойством и встаёт на его место в вагоне.
// Если не влез — появляется над локомотивом, как скрафченный.
public class DisassemblePresenter : IDisposable
{
    private readonly CargoSelectionModel selection;
    private readonly JourneyModel journey;
    private readonly WorkbenchModel workbench;

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
        if (!journey.DisassembleCargo(out var machine, out var trait)) return;
        if (wagon != null && wagon.TryPlace(machine, cell, trait)) return;
        workbench.TryGive(machine, trait);
    }

    public void Dispose() => selection.Changed -= OnSelected;
}
