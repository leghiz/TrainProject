using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public enum JourneyState
{
    Map,
    Travelling,
    Station,
    Finished,
    GameOver
}

public class JourneyModel
{
    public JourneyConfig Config { get; }
    public TrainModel Train { get; }
    public StormModel Storm { get; }
    public ResourceStorageModel Storage { get; }
    public MapModel Map { get; }

    public JourneyState State { get; private set; } = JourneyState.Map;
    public int Node => Map.Current.Column;
    public MapNode Target { get; private set; }
    public StationModel CurrentStation { get; private set; }
    public float TimeLeft { get; private set; }
    public DeliveryModel Delivery { get; } = new DeliveryModel();

    private readonly List<TravelEventConfig> activeEvents = new List<TravelEventConfig>();
    public IReadOnlyList<TravelEventConfig> ActiveEvents => activeEvents;

    public event Action<JourneyState> StateChanged;
    public event Action<int> NodeReached;

    private readonly Random random;
    private float pendingRainDamage;

    public JourneyModel(JourneyConfig config, TrainModel train, StormModel storm, ResourceStorageModel storage)
    {
        Config = config;
        Train = train;
        Storm = storm;
        Storage = storage;
        random = config.seed == 0 ? new Random() : new Random(config.seed);
        Map = new MapModel(config, random);
        storage.Add(ResourceType.Wood, config.startWood);
        storage.Add(ResourceType.Scrap, config.startScrap);
        train.Locomotive.Destroyed += OnLocomotiveDestroyed;
    }
    
    public bool Travel(MapNode target)
    {
        if (State != JourneyState.Map || !Map.CanMoveTo(target)) return false;
        Target = target;
        TimeLeft = Config.travelSeconds;
        activeEvents.Clear();
        activeEvents.AddRange(Map.Current.EventsTo(target));
        pendingRainDamage = 0f;
        SetState(JourneyState.Travelling);
        return true;
    }

    public bool HasEvent(TravelEventType type)
    {
        foreach (var travelEvent in activeEvents) if (travelEvent.type == type) return true;
        return false;
    }
    
    public bool AcceptCargo()
    {
        if (State != JourneyState.Station || Delivery.IsActive) return false;
        var offer = CurrentStation?.Cargo;
        if (offer == null) return false;

        var wagon = new WagonModel(offer.Wagon);
        Train.Attach(wagon);
        var box = wagon.TryPlaceBox(offer.Box, Vector2Int.zero);
        if (box == null) return false;

        int destination = Node + Config.deliveryDistance;
        foreach (var node in Map.ReachableAhead(Map.Current, Config.deliveryDistance))
            node.SetStation(Config.deliveryStation);

        CurrentStation.TakeCargo();
        Delivery.Start(box, wagon, destination);
        return true;
    }
    
    public bool DisassembleCargo(out MachineConfig machine, out MachineTrait trait) =>
        Delivery.Disassemble(Config.disassemblyMachines, Config.disassemblyTraits, random, out machine, out trait);
    
    private bool CanOfferCargo => !Delivery.IsActive && Node + Config.deliveryDistance < Config.nodeCount;

    public void LeaveStation()
    {
        if (State != JourneyState.Station) return;
        CurrentStation = null;
        SetState(JourneyState.Map);
    }

    public void Tick(float deltaTime)
    {
        if (State != JourneyState.Travelling) return;

        Train.Tick(deltaTime, Storage, WearMultiplier());
        if (State != JourneyState.Travelling) return;

        Storm.Tick(deltaTime, Train);
        if (State != JourneyState.Travelling) return;

        ApplyRain(deltaTime);

        TimeLeft -= deltaTime;
        if (TimeLeft <= 0f) Arrive();
    }

    private void Arrive()
    {
        TimeLeft = 0f;
        Map.MoveTo(Target);
        Target = null;
        activeEvents.Clear();
        NodeReached?.Invoke(Node);

        if (!Delivery.TryDeliver(Map.Current, Config.deliveryStation, Storage))
            Delivery.CheckMissed(Map.Current);

        var station = Map.Current.Station;
        if (station == null || station.isFinal || Node >= Config.nodeCount)
        {
            SetState(JourneyState.Finished);
            return;
        }

        Storm.NextNode();
        CurrentStation = new StationModel(station, random, CanOfferCargo);
        SetState(JourneyState.Station);
    }
    
    private float WearMultiplier()
    {
        float multiplier = 1f;
        foreach (var travelEvent in activeEvents)
            if (travelEvent.type == TravelEventType.Wind) multiplier *= travelEvent.wearMultiplier;
        return multiplier;
    }
    
    private void ApplyRain(float deltaTime)
    {
        float perSecond = 0f;
        foreach (var travelEvent in activeEvents)
            if (travelEvent.type == TravelEventType.Rain) perSecond += travelEvent.machineDamagePerSecond;
        if (perSecond <= 0f) return;

        pendingRainDamage += perSecond * deltaTime;
        int damage = Mathf.FloorToInt(pendingRainDamage);
        if (damage <= 0) return;
        pendingRainDamage -= damage;

        var wagons = new List<WagonModel>(Train.Wagons);
        foreach (var wagon in wagons) wagon.TakeRain(damage);
    }

    private void OnLocomotiveDestroyed() => SetState(JourneyState.GameOver);

    private void SetState(JourneyState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(state);
    }
}
