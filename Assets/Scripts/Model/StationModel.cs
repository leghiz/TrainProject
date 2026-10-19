using System;
using System.Collections.Generic;

public class WagonOffer
{
    public WagonConfig Config { get; }
    public WagonCondition Condition { get; } 

    public WagonOffer(WagonConfig config, WagonCondition condition)
    {
        Config = config;
        Condition = condition;
    }
}


public class CargoOffer
{
    public CargoBoxConfig Box { get; }
    public WagonConfig Wagon { get; }

    public CargoOffer(CargoBoxConfig box, WagonConfig wagon)
    {
        Box = box;
        Wagon = wagon;
    }
}

public class StationModel
{
    public StationConfig Config { get; }
    
    // Посылки на выбор: взять можно только одну.
    private readonly List<CargoOffer> cargoOffers = new List<CargoOffer>();
    public IReadOnlyList<CargoOffer> CargoOffers => cargoOffers;

    private readonly List<WagonOffer> offers = new List<WagonOffer>();
    public IReadOnlyList<WagonOffer> Offers => offers;

    public int PicksLeft { get; private set; }
    public bool CanPick => PicksLeft > 0 && offers.Count > 0;

    public event Action Changed;

    public StationModel(StationConfig config, Random random, bool offerCargo = false)
    {
        Config = config;
        if (offerCargo) PickCargo(config, random);
        
        var pool = new List<WagonConfig>();
        if (config.wagons != null)
            foreach (var wagon in config.wagons) if (wagon != null) pool.Add(wagon);

        for (int i = 0; i < config.offerCount && pool.Count > 0; i++)
        {
            int index = random.Next(pool.Count);
            offers.Add(new WagonOffer(pool[index], PickCondition(config, random)));
            pool.RemoveAt(index);
        }

        PicksLeft = Math.Min(config.pickCount, offers.Count);
    }
    
    public WagonModel Preview(int index) => CreateWagon(offers[index]);
    
    public WagonModel Pick(int index, TrainModel train)
    {
        if (!CanPick || index < 0 || index >= offers.Count) return null;

        var wagon = CreateWagon(offers[index]);
        offers.RemoveAt(index);
        PicksLeft--;
        train.Attach(wagon);
        Changed?.Invoke();
        return wagon;
    }
    
    public void TakeCargo()
    {
        cargoOffers.Clear();
        Changed?.Invoke();
    }

    // Несколько разных случайных ящиков из пула станции; к каждому — вагончик, куда он влезает.
    private void PickCargo(StationConfig config, Random random)
    {
        if (config.cargoBoxes == null || config.cargoWagons == null) return;

        var pool = new List<CargoBoxConfig>();
        foreach (var box in config.cargoBoxes) if (box != null && !pool.Contains(box)) pool.Add(box);

        while (cargoOffers.Count < config.cargoChoiceCount && pool.Count > 0)
        {
            int index = random.Next(pool.Count);
            var box = pool[index];
            pool.RemoveAt(index);

            var fitting = new List<WagonConfig>();
            foreach (var wagon in config.cargoWagons)
                if (wagon != null && box.size.x <= wagon.size.x && box.size.y <= wagon.size.y) fitting.Add(wagon);
            if (fitting.Count > 0) cargoOffers.Add(new CargoOffer(box, fitting[random.Next(fitting.Count)]));
        }
    }

    private WagonModel CreateWagon(WagonOffer offer) =>
        new WagonModel(offer.Config, offer.Condition, Config.woodMultiplier, Config.scrapMultiplier);
    
    private static WagonCondition PickCondition(StationConfig config, Random random)
    {
        if (config.conditions == null || config.conditions.Length == 0) return null;

        int total = 0;
        foreach (var entry in config.conditions) total += entry.weight;
        if (total <= 0) return null;

        int roll = random.Next(total);
        foreach (var entry in config.conditions)
        {
            if (roll < entry.weight) return entry.condition;
            roll -= entry.weight;
        }
        return null;
    }
}
