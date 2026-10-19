using System;
using UnityEngine;

// Экран станции: вагоны и посылки в ряд. Наведение — тултип вагона, клик — взять.
// Взятый вагон вставляется в выбранное место состава; когда брать больше нечего — поезд сам уезжает.
// «Cancel» — уехать, ничего не взяв.
public class StationScreenPresenter : IDisposable
{
    // Сколько секунд смотрим на состав с новым вагоном, прежде чем ехать дальше.
    private const float ShowInsertedSeconds = 0.6f;

    private readonly JourneyModel journey;
    private readonly StationScreenView view;
    private readonly HoverModel hover;
    private StationModel station;
    private Action<int> insert;

    public StationScreenPresenter(JourneyModel journey, StationScreenView view, HoverModel hover)
    {
        this.journey = journey;
        this.view = view;
        this.hover = hover;

        view.LeaveClicked += Leave;
        view.Train.Chosen += OnPlaceChosen;
        view.Train.Cancelled += OnInsertCancelled;
        journey.StateChanged += OnStateChanged;
        OnStateChanged(journey.State);
    }

    private void OnStateChanged(JourneyState state)
    {
        if (station != null) station.Changed -= Refresh;
        station = state == JourneyState.Station ? journey.CurrentStation : null;
        insert = null;

        if (station == null)
        {
            hover.Clear(this);
            view.ClearOffers();
            view.Hide();
            return;
        }
        station.Changed += Refresh;
        view.Show();
        view.SetInserting(false);
        Refresh();
    }

    private bool CargoAvailable => station.CargoOffers.Count > 0 && !journey.Delivery.IsActive;

    private void Refresh()
    {
        if (insert != null) return;
        hover.Clear(this);
        view.ClearOffers();

        int index = 0;
        for (int i = 0; i < station.Offers.Count; i++)
        {
            var offer = station.Offers[i];
            var preview = station.Preview(i);
            var material = offer.Condition != null ? offer.Condition.material : null;
            var item = view.AddOffer();
            item.Setup(offer.Config.sprite, material, view.AppearDelay(index++));
            item.SetInteractable(station.CanPick);
            item.Hovered += () => hover.Show(this, () => WagonTooltip(preview, item), item.TooltipPoint);
            item.Unhovered += () => hover.Clear(this);
            int pick = i;
            item.Clicked += () => StartInsert(item, offer.Config.sprite, material, null, default,
                at => station.Pick(pick, journey.Train, at));
        }

        if (CargoAvailable)
        {
            int stops = journey.Config.deliveryDistance;
            for (int i = 0; i < station.CargoOffers.Count; i++)
            {
                var offer = station.CargoOffers[i];
                var item = view.AddOffer();
                item.Setup(offer.Wagon.sprite, null, view.AppearDelay(index++));
                item.SetCargo(offer.Box.sprite, offer.Box.size, view.BlankSprite);
                var box = offer.Box;
                item.Hovered += () => hover.Show(this, () => TooltipTexts.CargoOffer(box, stops), item.TooltipPoint);
                item.Unhovered += () => hover.Clear(this);
                int take = i;
                item.Clicked += () => StartInsert(item, offer.Wagon.sprite, null, box.sprite, box.size,
                    at => journey.AcceptCargo(take, at));
            }
        }

        view.Layout();
    }

    private static TooltipContent WagonTooltip(WagonModel preview, StationOfferView item)
    {
        var content = TooltipTexts.Wagon(preview);
        content.Size = item.Size;
        return content;
    }

    // Вагон взят: остальные разлетаются, снизу — наш состав, куда его вставить.
    private void StartInsert(StationOfferView item, Sprite sprite, Material material,
                             Sprite cargo, Vector2Int cargoSize, Action<int> commit)
    {
        if (station == null || insert != null) return;
        insert = commit;
        hover.Clear(this);

        var train = view.Train;
        view.SetInserting(true);
        train.SetLocomotive(journey.Train.Locomotive.Config.sprite);
        foreach (var wagon in journey.Train.Wagons)
        {
            var car = train.AddWagon(wagon.Config.sprite, wagon.Condition != null ? wagon.Condition.material : null);
            foreach (var machine in wagon.Machines)
                train.AddPart(car, machine.Config.sprite, machine.Trait != null ? machine.Trait.material : null, machine.Cell, machine.Config.size);
            foreach (var box in wagon.Boxes)
                train.AddPart(car, box.Config.sprite, null, box.Cell, box.Config.size);
        }
        var carried = train.Carry(sprite, material, item.transform.position);
        if (cargoSize != default) train.AddPart(carried, cargo, null, Vector2Int.zero, cargoSize);
        view.Scatter(item);
    }

    private void OnPlaceChosen(int at)
    {
        if (insert == null) return;
        var commit = insert;
        commit(at);
        view.After(ShowInsertedSeconds, FinishInsert);
    }

    private void FinishInsert()
    {
        insert = null;
        if (station == null) return;
        if (!station.CanPick && !CargoAvailable)
        {
            Leave();
            return;
        }
        view.SetInserting(false);
        Refresh();
    }

    private void OnInsertCancelled()
    {
        insert = null;
        if (station == null) return;
        view.SetInserting(false);
        Refresh();
    }

    private void Leave()
    {
        hover.Clear(this);
        journey.LeaveStation();
    }

    public void Dispose()
    {
        if (station != null) station.Changed -= Refresh;
        hover.Clear(this);
        view.LeaveClicked -= Leave;
        view.Train.Chosen -= OnPlaceChosen;
        view.Train.Cancelled -= OnInsertCancelled;
        journey.StateChanged -= OnStateChanged;
    }
}
