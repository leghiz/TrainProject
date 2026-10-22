using System;
using UnityEngine;

// Экран станции: вагоны и посылки в ряд. Наведение — тултип вагона, клик — взять.
// Взятый вагон вставляется в выбранное место состава; когда брать больше нечего — поезд сам уезжает.
// «Cancel» — уехать, ничего не взяв.
// Довезли посылку — сначала награда: ящик улетает вверх, сверху падают станок и слиток. Станок берём в руку и ставим
// в вагон (экран на это время прячется), слиток жмём — он летит в счётчик. Пока награду не забрали, вагонов не видно.
public class StationScreenPresenter : IDisposable
{
    // Сколько секунд смотрим на состав с новым вагоном, прежде чем ехать дальше.
    private const float ShowInsertedSeconds = 0.6f;

    private readonly JourneyModel journey;
    private readonly StationScreenView view;
    private readonly HoverModel hover;
    private readonly ArrivalView arrival;
    private readonly BuildModel build;
    private DeliveryReward reward;
    private bool placingReward;
    private StationModel station;
    private Action<int> insert;

    public StationScreenPresenter(JourneyModel journey, StationScreenView view, HoverModel hover,
                                  ArrivalView arrival = null, BuildModel build = null)
    {
        this.journey = journey;
        this.view = view;
        this.hover = hover;
        this.arrival = arrival;
        this.build = build;

        view.LeaveClicked += Leave;
        view.Train.Chosen += OnPlaceChosen;
        view.Train.Cancelled += OnInsertCancelled;
        if (view.Reward != null)
        {
            view.Reward.MachineClicked += TakeRewardMachine;
            view.Reward.IngotArrived += OnRewardIngotArrived;
        }
        if (build != null)
        {
            build.SelectedChanged += OnBuildChanged;
            build.Cancelled += OnBuildCancelled;
        }
        journey.StateChanged += OnStateChanged;
        OnStateChanged(journey.State);
    }

    private void OnStateChanged(JourneyState state)
    {
        if (station != null) station.Changed -= Refresh;
        station = state == JourneyState.Station ? journey.CurrentStation : null;
        insert = null;
        reward = null;
        placingReward = false;

        if (station == null)
        {
            if (arrival != null) arrival.Stop();
            hover.Clear(this);
            view.ClearOffers();
            view.Hide();
            return;
        }
        if (arrival != null) arrival.Play(() => Open(true));
        else Open(false);
    }

    // Станция открывается после прибытия: камера наехала на локомотив, экран потемнел.
    private void Open(bool darkAlready)
    {
        if (station == null) return;
        station.Changed -= Refresh;
        station.Changed += Refresh;
        view.Show(darkAlready);
        view.SetInserting(false);
        reward = journey.Delivery.Reward;
        if (reward != null && !reward.IsDone && view.Reward != null)
        {
            ShowReward(true);
            return;
        }
        reward = null;
        Refresh();
    }

    // Награда за доставку: пока её не забрали, вагонов нет и уехать нельзя.
    private void ShowReward(bool withBox)
    {
        hover.Clear(this);
        view.ClearOffers();
        view.SetLeaveVisible(false);
        var machine = reward.Machine;
        view.Reward.Play(reward.Box.sprite, machine != null ? machine.sprite : null,
                         reward.Trait != null ? reward.Trait.material : null, reward.Ingots, withBox,
                         machine != null && !reward.MachineTaken, !reward.IngotsTaken);
    }

    // Станок из награды — в руку: экран прячется, ставим его в настоящий поезд.
    private void TakeRewardMachine()
    {
        if (reward == null || reward.MachineTaken || reward.Machine == null || build == null || !build.IsEmpty) return;
        placingReward = true;
        view.Hide();
        build.Select(reward.Machine, reward.Trait);
    }

    // Передумали (ПКМ) — станок возвращается на экран станции.
    private void OnBuildCancelled(MachineConfig machine, MachineTrait trait)
    {
        if (!placingReward) return;
        placingReward = false;
        Reopen();
    }

    private void OnBuildChanged(MachineConfig machine)
    {
        if (!placingReward || !build.IsEmpty) return;
        placingReward = false;
        reward.MachineTaken = true;
        Reopen();
    }

    private void Reopen()
    {
        if (station == null) return;
        view.Show();
        view.SetInserting(false);
        if (reward != null && !reward.IsDone) ShowReward(false);
        else FinishReward();
    }

    private void OnRewardIngotArrived()
    {
        journey.CollectDeliveryIngots();
        if (reward != null && reward.IsDone) FinishReward();
    }

    private void FinishReward()
    {
        reward = null;
        journey.Delivery.ClearReward();
        if (view.Reward != null) view.Reward.Hide();
        view.SetLeaveVisible(true);
        Refresh();
    }

    private bool CargoAvailable => station.CargoOffers.Count > 0 && !journey.Delivery.IsActive;

    private void Refresh()
    {
        if (insert != null || reward != null) return;
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
                item.Setup(offer.Wagon.sprite, offer.Condition != null ? offer.Condition.material : null, view.AppearDelay(index++));
                item.SetCargo(offer.Box.sprite, offer.Box.size, view.BlankSprite);
                var box = offer.Box;
                var preview = new WagonModel(offer.Wagon, offer.Condition);
                preview.TryPlaceBox(box, Vector2Int.zero);
                item.Hovered += () => hover.Show(this, () => CargoTooltip(preview, box, stops, item), item.TooltipPoint);
                item.Unhovered += () => hover.Clear(this);
                int take = i;
                item.Clicked += () => StartInsert(item, offer.Wagon.sprite, null, box.sprite, box.size,
                    at => journey.AcceptCargo(take, at));
            }
        }

        view.Layout();
    }

    private static TooltipContent CargoTooltip(WagonModel preview, CargoBoxConfig box, int stops, StationOfferView item)
    {
        var content = WagonTooltip(preview, item);
        string delivery = $"Deliver in {stops} stops: +{box.rewardIngots} ingots";
        content.Effects = string.IsNullOrEmpty(content.Effects) ? delivery : content.Effects + "\n" + delivery;
        return content;
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
        if (view.Reward != null)
        {
            view.Reward.MachineClicked -= TakeRewardMachine;
            view.Reward.IngotArrived -= OnRewardIngotArrived;
        }
        if (build != null)
        {
            build.SelectedChanged -= OnBuildChanged;
            build.Cancelled -= OnBuildCancelled;
        }
        journey.StateChanged -= OnStateChanged;
    }
}
