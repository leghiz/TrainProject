using System;

public class BoilerUpgradePresenter : IDisposable
{
    private readonly LocomotiveModel locomotive;
    private readonly ResourceStorageModel storage;
    private readonly BoilerUpgradeView view;

    public BoilerUpgradePresenter(LocomotiveModel locomotive, ResourceStorageModel storage, BoilerUpgradeView view)
    {
        this.locomotive = locomotive;
        this.storage = storage;
        this.view = view;

        Refresh();
        storage.Changed += Refresh;
        locomotive.BoilerLevelChanged += OnLevelChanged;
        view.Clicked += OnClicked;
    }

    private void OnClicked() => locomotive.TryUpgradeBoiler(storage);

    private void OnLevelChanged(int level) => Refresh();
    
    private void Refresh()
    {
        if (!locomotive.CanUpgradeBoiler) view.Hide();
        else view.Set($"↑ {locomotive.UpgradeCost}", locomotive.CanAffordUpgrade(storage));
    }

    public void Dispose()
    {
        storage.Changed -= Refresh;
        locomotive.BoilerLevelChanged -= OnLevelChanged;
        view.Clicked -= OnClicked;
    }
}
