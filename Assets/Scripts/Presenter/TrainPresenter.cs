using System;
using System.Collections.Generic;
public class TrainPresenter : IDisposable
{
    private readonly TrainModel model;
    private readonly TrainView view;
    private readonly BuildModel build;
    private readonly ResourceStorageModel storage;
    private readonly LocomotivePresenter locomotivePresenter;

    private readonly Dictionary<WagonModel, (WagonPresenter presenter, WagonView view)> wagons =
        new Dictionary<WagonModel, (WagonPresenter presenter, WagonView view)>();

    public TrainPresenter(TrainModel model, TrainView view, BuildModel build, ResourceStorageModel storage)
    {
        this.model = model;
        this.view = view;
        this.build = build;
        this.storage = storage;

        locomotivePresenter = new LocomotivePresenter(model.Locomotive, view.Locomotive, build, storage);
        foreach (var wagon in model.Wagons) ShowWagon(wagon);
        view.Arrange();

        model.WagonAttached += OnWagonAttached;
        model.WagonDetached += OnWagonDetached;
    }

    private void ShowWagon(WagonModel wagon)
    {
        var wagonView = view.AddWagon();
        wagons[wagon] = (new WagonPresenter(wagon, wagonView, build, storage), wagonView);
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
