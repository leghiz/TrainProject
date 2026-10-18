using UnityEngine;
using UnityEngine.InputSystem;

public class WagonSandbox : MonoBehaviour
{
    [SerializeField, Expandable] private WagonConfig[] wagons;
    [SerializeField, Expandable] private MachineConfig[] machines;
    [SerializeField, Expandable] private LocomotiveConfig locomotiveConfig;
    [SerializeField] private TrainView trainView;
    [SerializeField, Expandable] private StormConfig stormConfig;
    [SerializeField] private StormView stormPrefab;
    [SerializeField, Expandable] private JourneyConfig journeyConfig;

    private readonly BuildModel build = new BuildModel();
    private readonly ResourceStorageModel storage = new ResourceStorageModel();

    private LocomotiveModel locomotive;
    private TrainModel train;
    private TrainPresenter trainPresenter;
    private StormModel storm;
    private StormPresenter stormPresenter;
    private JourneyModel journey;
    private JourneyPresenter journeyPresenter;
    private float logTimer;

    private WagonModel Tail => train.Wagons.Count > 0 ? train.Wagons[train.Wagons.Count - 1] : null;

    private void Start()
    {
        for (int i = 0; i < machines.Length; i++) Debug.Log($"{i + 1} — {machines[i].displayName}");
        storage.Changed += LogStorage;

        locomotive = new LocomotiveModel(locomotiveConfig);
        locomotive.Destroyed += () => Debug.Log("[Локомотив] разрушен — конец игры");

        train = new TrainModel(locomotive);
        train.WagonAttached += w => Debug.Log($"[Поезд] прицеплен {w.Config.displayName}");
        train.WagonDetached += w => Debug.Log($"[Поезд] отцеплен {w.Config.displayName}");

        trainPresenter = new TrainPresenter(train, trainView, build);
        AttachRandomWagon();

        storm = new StormModel(stormConfig, train);
        storm.NodeChanged += node => Debug.Log($"[Буря] узел {node}, скорость {storm.Speed:0.00}");
        var stormView = Instantiate(stormPrefab, trainView.transform);
        stormPresenter = new StormPresenter(storm, stormView, trainView);

        journey = new JourneyModel(journeyConfig, train, storm, storage);
        journey.StateChanged += LogState;
        journeyPresenter = new JourneyPresenter(journey, trainView);
        journey.NodeReached += node => Debug.Log($"[Забег] доехали до узла {node} из {journeyConfig.nodeCount}: {NodeName(journey.Map.Current)}");
        LogMap();
        LogChoices();
    }

    private void Update()
    {
        journey.Tick(Time.deltaTime);

        logTimer -= Time.deltaTime;
        if (logTimer <= 0f && journey.State == JourneyState.Travelling) { logTimer = 2f; LogTrain(); }

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.enterKey.wasPressedThisFrame) journey.LeaveStation();
        for (int i = 0; i < 5; i++)
        {
            if (kb[Key.F1 + i].wasPressedThisFrame) OnFKey(i);
        }
        if (kb.dKey.wasPressedThisFrame) Tail?.Damage(10);
        if (kb.rKey.wasPressedThisFrame) Tail?.Repair(10);
        if (kb.kKey.wasPressedThisFrame) locomotive.Damage(10);
        if (kb.lKey.wasPressedThisFrame) LoadMachines();
        if (kb.uKey.wasPressedThisFrame)
            Debug.Log(locomotive.UpgradeBoiler() ? $"[Коптильня] улучшена до уровня {locomotive.BoilerLevel}" : "[Коптильня] уже максимальный уровень");

        for (int i = 0; i < machines.Length && i < 9; i++)
        {
            if (kb[Key.Digit1 + i].wasPressedThisFrame)
            {
                build.Select(machines[i]);
                Debug.Log($"Выбран: {machines[i].displayName}");
            }
        }
        if (kb.escapeKey.wasPressedThisFrame)
        {
            build.Select(null);
            Debug.Log("Выбор снят");
        }
    }

    private void OnDestroy()
    {
        journeyPresenter?.Dispose();
        stormPresenter?.Dispose();
        trainPresenter?.Dispose();
    }

    private void AttachRandomWagon() =>
        train.Attach(new WagonModel(wagons[Random.Range(0, wagons.Length)]));

    // F1..F5: на карте — выбрать, куда ехать; на станции — взять вагон.
    private void OnFKey(int index)
    {
        if (journey.State == JourneyState.Map)
        {
            var choices = journey.Map.Choices;
            if (index < choices.Count) journey.Travel(choices[index]);
        }
        else if (journey.State == JourneyState.Station)
        {
            var station = journey.CurrentStation;
            if (!station.CanPick) { Debug.Log("[Станция] больше брать нельзя. Enter — к карте"); return; }
            var wagon = station.Pick(index, train);
            if (wagon != null) LogOffers();
        }
    }

    private static string NodeName(MapNode node) => node.Station != null ? node.Station.displayName : "Старт";
    
    private void LogMap()
    {
        var sb = new System.Text.StringBuilder("[Карта]\n");
        foreach (var column in journey.Map.Columns)
        {
            sb.Append($"{column[0].Column,2}: ");
            foreach (var node in column)
            {
                sb.Append($"[{node.Row}] {NodeName(node)}");
                if (node.Next.Count > 0)
                {
                    sb.Append(" → ");
                    for (int i = 0; i < node.Next.Count; i++)
                        sb.Append((i == 0 ? "" : ",") + node.Next[i].Row + EventsText(node.EventsTo(node.Next[i]), "(", ")"));
                }
                sb.Append("   ");
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }

    private void LogChoices()
    {
        var choices = journey.Map.Choices;
        var sb = new System.Text.StringBuilder($"[Карта] стоим: {NodeName(journey.Map.Current)} (узел {journey.Node}). Куда едем:");
        for (int i = 0; i < choices.Count; i++)
            sb.Append($"  F{i + 1} — {NodeName(choices[i])}{EventsText(journey.Map.Current.EventsTo(choices[i]), " [", "]")}");
        Debug.Log(sb.ToString());
    }

    private static string EventsText(System.Collections.Generic.IReadOnlyList<TravelEventConfig> events, string open, string close)
    {
        if (events.Count == 0) return "";
        var names = new string[events.Count];
        for (int i = 0; i < events.Count; i++) names[i] = events[i].displayName;
        return open + string.Join(", ", names) + close;
    }

    private void LogOffers()
    {
        var station = journey.CurrentStation;
        var sb = new System.Text.StringBuilder($"[Станция] {station.Config.displayName}, можно взять ещё {station.PicksLeft}:");
        for (int i = 0; i < station.Offers.Count; i++)
        {
            var offer = station.Offers[i];
            var preview = station.Preview(i);
            string condition = offer.Condition != null ? offer.Condition.displayName : "Обычный";
            sb.Append($"  F{i + 1} — {offer.Config.displayName} ({condition}, дерево {preview.Wood}, лом {preview.Scrap}, вес {preview.Weight:0.#})");
        }
        sb.Append("   | Enter — к карте");
        Debug.Log(sb.ToString());
    }

    private void LoadMachines()
    {
        foreach (var wagon in train.Wagons)
            foreach (var machine in wagon.Machines) LoadMachine(machine);
        if (locomotive.Boiler != null) LoadMachine(locomotive.Boiler);
    }

    private void LoadMachine(MachineModel machine)
    {
        if (!machine.NeedsLoading) return;
        int loaded = machine.Load(storage, 4);
        Debug.Log($"[Загрузка] {machine.Config.displayName}: +{loaded} {machine.Config.resource}, в станке {machine.Loaded}");
    }

    private void LogTrain()
    {
        if (!locomotive.HasLevels) return;
        var level = locomotive.Level;
        int fuel = locomotive.Boiler != null ? locomotive.Boiler.Loaded : 0;
        Debug.Log($"[Поезд] вагонов {train.Wagons.Count}, клетки {train.Cells}, вес {train.Weight:0.#}, тяга {level.traction}, скорость {train.Speed:0.00}, " +
                  $"коптильня ур.{locomotive.BoilerLevel}, топливо {fuel}/{level.tank}, хп локомотива {locomotive.Hp} | " +
                  $"буря: скорость {storm.Speed:0.00}, до хвоста {storm.DistanceToTail(train):0.0} клеток | " +
                  $"едем к {(journey.Target != null ? NodeName(journey.Target) : "?")} (узел {journey.Node + 1}), осталось {journey.TimeLeft:0} с");
    }

    private void LogState(JourneyState state)
    {
        switch (state)
        {
            case JourneyState.Travelling:
                string events = journey.ActiveEvents.Count > 0 ? EventsText(journey.ActiveEvents, " — события: ", "") : " — без событий";
                Debug.Log($"[Забег] Едем: {NodeName(journey.Target)} (узел {journey.Node + 1}), {journeyConfig.travelSeconds:0} с{events}");
                break;
            case JourneyState.Station: LogOffers(); break;
            case JourneyState.Map: LogChoices(); break;
            case JourneyState.Finished: Debug.Log("[Забег] Конечная! Конец акта"); break;
            case JourneyState.GameOver: Debug.Log("[Забег] Поражение: локомотив разрушен"); break;
        }
    }

    private void LogStorage() =>
        Debug.Log($"[Склад] дерево {storage.Get(ResourceType.Wood)}, лом {storage.Get(ResourceType.Scrap)}, слитки {storage.Get(ResourceType.Ingot)}");
}
