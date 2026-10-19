using System;

public class StationScreenPresenter : IDisposable
{
    private readonly JourneyModel journey;
    private readonly StationScreenView view;
    private StationModel station;

    public StationScreenPresenter(JourneyModel journey, StationScreenView view)
    {
        this.journey = journey;
        this.view = view;

        view.TakeCargoClicked += OnTakeCargo;
        view.LeaveClicked += journey.LeaveStation;
        journey.StateChanged += OnStateChanged;
        OnStateChanged(journey.State);
    }

    private void OnStateChanged(JourneyState state)
    {
        if (station != null) station.Changed -= Refresh;
        station = state == JourneyState.Station ? journey.CurrentStation : null;

        if (station == null)
        {
            view.Hide();
            return;
        }
        station.Changed += Refresh;
        view.Show();
        Refresh();
    }

    private void Refresh()
    {
        view.SetTitle(station.Config.displayName);
        view.SetPicksLeft(station.CanPick ? $"можно взять: {station.PicksLeft}" : "больше брать нельзя");

        view.ClearCards();
        int count = station.Offers.Count;
        for (int i = 0; i < count; i++)
        {
            var offer = station.Offers[i];
            var preview = station.Preview(i);
            string condition = offer.Condition != null ? offer.Condition.displayName : "Обычный";
            var card = view.AddCard(i, count);
            card.Setup(offer.Config.sprite, offer.Config.displayName,
                $"{condition}\nдерево {preview.Wood}, лом {preview.Scrap}\nвес {preview.Weight:0.#}");
            card.SetInteractable(station.CanPick);
            int index = i;
            card.Clicked += () => station.Pick(index, journey.Train);
        }

        var cargo = station.Cargo;
        if (cargo != null && !journey.Delivery.IsActive)
            view.ShowCargo($"{cargo.Box.displayName} в вагоне {cargo.Wagon.displayName}\n" +
                           $"довезти до узла {journey.Node + journey.Config.deliveryDistance}, награда {cargo.Box.rewardIngots} слитков");
        else view.HideCargo();
    }
    private void OnTakeCargo() => journey.AcceptCargo();

    public void Dispose()
    {
        if (station != null) station.Changed -= Refresh;
        view.TakeCargoClicked -= OnTakeCargo;
        view.LeaveClicked -= journey.LeaveStation;
        journey.StateChanged -= OnStateChanged;
    }
}
