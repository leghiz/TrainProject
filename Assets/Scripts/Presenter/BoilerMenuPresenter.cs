using System;

// Клик по коптильне — меню: сколько дров в баке, «+» подкидывает дерево со склада, внизу — тяга.
public class BoilerMenuPresenter : IDisposable
{
    private readonly LocomotiveModel locomotive;
    private readonly TrainModel train;
    private readonly ResourceStorageModel storage;
    private readonly BuildModel build;
    private readonly LocomotiveView locomotiveView;
    private readonly BoilerMenuView view;

    public BoilerMenuPresenter(LocomotiveModel locomotive, TrainModel train, ResourceStorageModel storage, BuildModel build,
                               LocomotiveView locomotiveView, BoilerMenuView view)
    {
        this.locomotive = locomotive;
        this.train = train;
        this.storage = storage;
        this.build = build;
        this.locomotiveView = locomotiveView;
        this.view = view;

        view.Hide();
        locomotiveView.BoilerClicked += Toggle;
        locomotiveView.WorkbenchClicked += Close;
        locomotive.Destroyed += Close;
        view.AddClicked += AddWood;
        view.UpgradeClicked += Upgrade;
        view.CloseClicked += Close;
        locomotiveView.FoggedChanged += OnFogged;
    }

    // В тумане камера ушла к другому вагону — меню коптильни закрывается.
    private void OnFogged(bool fogged)
    {
        if (fogged && view.IsOpen) Close();
    }

    // Каждый кадр, пока меню открыто: дрова горят, вагоны цепляются — числа живые.
    public void Tick()
    {
        if (view.IsOpen) Refresh();
    }

    private void Toggle()
    {
        if (view.IsOpen)
        {
            Close();
            return;
        }
        if (!build.IsEmpty || locomotive.Boiler == null || locomotive.IsDestroyed) return;
        view.Show();
        locomotiveView.HideHp(this, true);
        locomotiveView.HighlightBoiler(true);
        locomotiveView.SuppressReady(true);
        if (locomotiveView.BoilerUpgrade != null) locomotiveView.BoilerUpgrade.SetSuppressed(true);
        Refresh();
    }

    private void Close()
    {
        view.Hide();
        locomotiveView.HideHp(this, false);
        locomotiveView.HighlightBoiler(false);
        locomotiveView.SuppressReady(false);
        if (locomotiveView.BoilerUpgrade != null) locomotiveView.BoilerUpgrade.SetSuppressed(false);
    }

    private void AddWood()
    {
        var boiler = locomotive.Boiler;
        if (boiler == null || locomotive.IsDestroyed) return;
        boiler.Load(storage, view.WoodPerClick);
        Refresh();
    }

    // «+» внизу меню: за слитки — следующий уровень коптильни (бак больше, едет быстрее, тянет больше).
    private void Upgrade()
    {
        if (locomotive.TryUpgradeBoiler(storage)) Refresh();
    }

    private void Refresh()
    {
        var boiler = locomotive.Boiler;
        if (boiler == null) return;
        view.SetFuel(boiler.Loaded, boiler.Capacity);
        bool roomLeft = boiler.Capacity <= 0 || boiler.Loaded < boiler.Capacity;
        view.SetCanAdd(roomLeft && storage.Has(ResourceType.Wood, 1));
        view.SetTraction(train.Weight, locomotive.HasLevels ? locomotive.Level.traction : 0);
        view.SetUpgrade(locomotive.UpgradeCost, locomotive.CanUpgradeBoiler, locomotive.CanAffordUpgrade(storage));
    }

    public void Dispose()
    {
        locomotiveView.BoilerClicked -= Toggle;
        locomotiveView.WorkbenchClicked -= Close;
        locomotive.Destroyed -= Close;
        view.AddClicked -= AddWood;
        view.UpgradeClicked -= Upgrade;
        view.CloseClicked -= Close;
        locomotiveView.FoggedChanged -= OnFogged;
    }
}
