using System;

// Клик по ящику посылки → над ним кнопка «Разобрать» → посылка провалена,
// а станок со случайным свойством появляется над локомотивом, как скрафченный.
public class DisassemblePresenter : IDisposable
{
    private readonly CargoSelectionModel selection;
    private readonly JourneyModel journey;
    private readonly WorkbenchModel workbench;
    private readonly WorldButtonView button;

    public DisassemblePresenter(CargoSelectionModel selection, JourneyModel journey, WorkbenchModel workbench, WorldButtonView button)
    {
        this.selection = selection;
        this.journey = journey;
        this.workbench = workbench;
        this.button = button;

        button.Hide();
        selection.Changed += Refresh;
        workbench.ReadyChanged += OnReadyChanged;
        button.Clicked += OnClicked;
        button.PressedOutside += selection.Clear;
        journey.Delivery.Failed += OnDeliveryEnded;
        journey.Delivery.Delivered += OnDeliveryEnded;
    }

    private void Refresh()
    {
        var box = selection.Box;
        if (box == null || box != journey.Delivery.Box)
        {
            button.Hide();
            return;
        }
        // Над локомотивом уже висит готовый станок — сначала его нужно забрать.
        button.ShowAt(selection.Anchor, "Разобрать", !workbench.HasReady);
    }

    private void OnReadyChanged(MachineConfig machine)
    {
        if (selection.Box != null) button.SetInteractable(!workbench.HasReady);
    }

    private void OnClicked()
    {
        if (workbench.HasReady) return;
        if (journey.DisassembleCargo(out var machine, out var trait)) workbench.TryGive(machine, trait);
        selection.Clear();
    }

    private void OnDeliveryEnded(string reason) => selection.Clear();

    private void OnDeliveryEnded(int ingots) => selection.Clear();

    public void Dispose()
    {
        selection.Changed -= Refresh;
        workbench.ReadyChanged -= OnReadyChanged;
        button.Clicked -= OnClicked;
        button.PressedOutside -= selection.Clear;
        journey.Delivery.Failed -= OnDeliveryEnded;
        journey.Delivery.Delivered -= OnDeliveryEnded;
    }
}
