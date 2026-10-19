using System;
using System.Collections.Generic;
public class TrainPresenter : IDisposable
{
    private readonly TrainModel model;
    private readonly TrainView view;
    private readonly BuildModel build;
    private readonly ResourceStorageModel storage;
    private readonly HoverModel hover;
    private readonly CargoSelectionModel cargoSelection;
    private readonly DismantleModel dismantle;
    private readonly LocomotivePresenter locomotivePresenter;

    private readonly Dictionary<WagonModel, (WagonPresenter presenter, WagonView view)> wagons =
        new Dictionary<WagonModel, (WagonPresenter presenter, WagonView view)>();

    public TrainPresenter(TrainModel model, TrainView view, BuildModel build, ResourceStorageModel storage,
                          HoverModel hover, CargoSelectionModel cargoSelection, DismantleModel dismantle)
    {
        this.model = model;
        this.view = view;
        this.build = build;
        this.storage = storage;
        this.hover = hover;
        this.cargoSelection = cargoSelection;
        this.dismantle = dismantle;

        locomotivePresenter = new LocomotivePresenter(model.Locomotive, view.Locomotive, build, storage, hover, dismantle);
        foreach (var wagon in model.Wagons) ShowWagon(wagon);
        view.Arrange();

        model.WagonAttached += OnWagonAttached;
        model.WagonDetached += OnWagonDetached;
    }

    private void ShowWagon(WagonModel wagon)
    {
        var wagonView = view.AddWagon(model.IndexOf(wagon));
        wagons[wagon] = (new WagonPresenter(wagon, wagonView, build, storage, hover, cargoSelection, dismantle), wagonView);
    }

    private void OnWagonAttached(WagonModel wagon)
    {
        ShowWagon(wagon);
        view.Arrange();
    }

    private void OnWagonDetached(WagonModel wagon)
    {
        if (!wagons.TryGetValue(wagon, out var entry)) return;
        entry.presenter.Dispose();
        view.RemoveWagon(entry.view);
        wagons.Remove(wagon);
        view.Arrange();
    }

    public void Dispose()
    {
        model.WagonAttached -= OnWagonAttached;
        model.WagonDetached -= OnWagonDetached;

        foreach (var entry in wagons.Values) entry.presenter.Dispose();
        wagons.Clear();
        locomotivePresenter.Dispose();
    }
}
