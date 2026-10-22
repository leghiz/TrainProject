using System;
using System.Collections.Generic;

// Звук по событиям игры: погода в пути (дождь, ветер), взяли что-то в руку, что-то поставили
// (станок или ящик в вагон, сварку или лист на локомотив, новый вагон в состав).
public class AudioPresenter : IDisposable
{
    private readonly JourneyModel journey;
    private readonly TrainModel train;
    private readonly BuildModel build;
    private readonly AudioView view;
    private readonly List<WagonModel> wagons = new List<WagonModel>();
    private bool holding;

    public AudioPresenter(JourneyModel journey, TrainModel train, BuildModel build, AudioView view)
    {
        this.journey = journey;
        this.train = train;
        this.build = build;
        this.view = view;

        holding = !build.IsEmpty;
        foreach (var wagon in train.Wagons) Follow(wagon);
        RefreshWeather(journey.State);

        journey.StateChanged += RefreshWeather;
        build.SelectedChanged += OnSelectedChanged;
        train.WagonAttached += OnWagonAttached;
        train.WagonDetached += Unfollow;
        train.Locomotive.MachinePlaced += OnMachinePlaced;
        train.Locomotive.Plating.PlateAdded += OnPlateAdded;
    }

    private void RefreshWeather(JourneyState state)
    {
        bool travelling = state == JourneyState.Travelling;
        view.SetTravelling(travelling);
        view.SetWeather(travelling && journey.HasEvent(TravelEventType.Rain),
                        travelling && journey.HasEvent(TravelEventType.Wind));
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        bool now = !build.IsEmpty;
        if (now && !holding) AudioView.Pickup();
        holding = now;
    }

    private void OnWagonAttached(WagonModel wagon)
    {
        Follow(wagon);
        AudioView.Place();
    }

    private void Follow(WagonModel wagon)
    {
        if (wagons.Contains(wagon)) return;
        wagons.Add(wagon);
        wagon.MachinePlaced += OnMachinePlaced;
        wagon.BoxPlaced += OnBoxPlaced;
    }

    private void Unfollow(WagonModel wagon)
    {
        if (!wagons.Remove(wagon)) return;
        wagon.MachinePlaced -= OnMachinePlaced;
        wagon.BoxPlaced -= OnBoxPlaced;
    }

    private void OnMachinePlaced(MachineModel machine) => AudioView.Place();

    private void OnBoxPlaced(CargoBoxModel box) => AudioView.Place();

    private void OnPlateAdded(PlateModel plate) => AudioView.Place();

    public void Dispose()
    {
        journey.StateChanged -= RefreshWeather;
        build.SelectedChanged -= OnSelectedChanged;
        train.WagonAttached -= OnWagonAttached;
        train.WagonDetached -= Unfollow;
        train.Locomotive.MachinePlaced -= OnMachinePlaced;
        train.Locomotive.Plating.PlateAdded -= OnPlateAdded;
        foreach (var wagon in wagons.ToArray()) Unfollow(wagon);
    }
}
