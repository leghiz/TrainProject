using System;

// Удержали ПКМ над ящиком посылки до конца → посылка провалена,
// а станок со случайным свойством появляется над локомотивом, как скрафченный.
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
        // Над локомотивом уже висит готовый станок — новый положить некуда, сначала забери тот.
        if (box != journey.Delivery.Box || workbench.HasReady) return;
        if (journey.DisassembleCargo(out var machine, out var trait)) workbench.TryGive(machine, trait);
    }

    public void Dispose() => selection.Changed -= OnSelected;
}
