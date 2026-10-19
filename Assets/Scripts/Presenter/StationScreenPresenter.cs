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
        
        var cargo = station.CargoOffers;
        bool showCargo = cargo.Count > 0 && !journey.Delivery.IsActive;
        view.SetCargoVisible(showCargo);
        if (!showCargo) return;
        int destination = journey.Node + journey.Config.deliveryDistance;
        for (int i = 0; i < cargo.Count; i++)
        {
            var box = cargo[i].Box;
            var card = view.AddCargoCard(i, cargo.Count);
            card.Setup(box.sprite, box.displayName,
                $"{box.size.x}×{box.size.y}, вес {box.weight:0.#}\nв вагоне {cargo[i].Wagon.displayName}\n" +
                $"до узла {destination}: {box.rewardIngots} слитков");
            int index = i;
            card.Clicked += () => journey.AcceptCargo(index);
        }
    }

    public void Dispose()
    {
        if (station != null) station.Changed -= Refresh;
        view.LeaveClicked -= journey.LeaveStation;
        journey.StateChanged -= OnStateChanged;
    }
}
