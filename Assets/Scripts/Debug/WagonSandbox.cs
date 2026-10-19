using UnityEngine;
using UnityEngine.InputSystem;

public class WagonSandbox : MonoBehaviour
{
    [SerializeField, Expandable] private WagonConfig[] wagons;
    [SerializeField, Expandable] private MachineConfig[] machines;
    [SerializeField, Expandable] private LocomotiveConfig locomotiveConfig;
    [SerializeField] private TrainView trainView;
    [SerializeField, Expandable] private StormConfig stormConfig;
    [Tooltip("Буря из сцены (дочерний объект Train): высоту и вид задаёт геймдиз, по X её двигает код")]
    [SerializeField] private StormView stormView;
    [SerializeField, Expandable] private JourneyConfig journeyConfig;
    [SerializeField, Expandable] private MachineTrait[] traits;
    [SerializeField] private WorkbenchPanelView workbenchPanel;
    [SerializeField] private HandView handView;
    [SerializeField] private ResourcePanelView resourcePanel;
    [SerializeField] private TooltipView tooltipView;
    [SerializeField] private MapScreenView mapScreen;
    [SerializeField] private StationScreenView stationScreen;

    private readonly BuildModel build = new BuildModel();
    private readonly ResourceStorageModel storage = new ResourceStorageModel();
    private readonly HoverModel hover = new HoverModel();

    private LocomotiveModel locomotive;
    private TrainModel train;
    private TrainPresenter trainPresenter;
    private StormModel storm;
    private StormPresenter stormPresenter;
    private JourneyModel journey;
    private JourneyPresenter journeyPresenter;
    private WorkbenchModel workbench;
    private WorkbenchPresenter workbenchPresenter;
    private HandPresenter handPresenter;
    private ResourcePanelPresenter resourcePanelPresenter;
    private TooltipPresenter tooltipPresenter;
    private MapScreenPresenter mapScreenPresenter;
    private StationScreenPresenter stationScreenPresenter;
    private float logTimer;
    private int traitIndex = -1; 

    private MachineTrait CurrentTrait => traitIndex >= 0 && traitIndex < traits.Length ? traits[traitIndex] : null;
    private string TraitName => CurrentTrait != null ? CurrentTrait.displayName : "без свойства";

    private WagonModel Tail => train.Wagons.Count > 0 ? train.Wagons[train.Wagons.Count - 1] : null;

    private void Start()
    {
        for (int i = 0; i < machines.Length; i++) Debug.Log($"{i + 1} — {machines[i].displayName}");
        storage.Changed += LogStorage;

        locomotive = new LocomotiveModel(locomotiveConfig);
        locomotive.Destroyed += () => Debug.Log("[Локомотив] разрушен — конец игры");
        locomotive.Plating.Changed += () => Debug.Log($"[Обшивка] листов {locomotive.Plating.Plates.Count}, хп {locomotive.Plating.TotalHp}/{locomotive.Plating.TotalMaxHp}");

        train = new TrainModel(locomotive);
        train.WagonAttached += w => Debug.Log($"[Поезд] прицеплен {w.Config.displayName}");
        train.WagonDetached += w => Debug.Log($"[Поезд] отцеплен {w.Config.displayName}");

        trainPresenter = new TrainPresenter(train, trainView, build, storage, hover);
        AttachRandomWagon();

        workbench = new WorkbenchModel(locomotiveConfig.recipes, storage);
        workbench.ReadyChanged += m => Debug.Log(m != null ? $"[Верстак] готов {m.displayName} — тащи иконку на вагон" : "[Верстак] слот свободен");
        workbenchPresenter = new WorkbenchPresenter(workbench, storage, build, trainView.Locomotive, workbenchPanel, hover);
        handPresenter = new HandPresenter(build, handView);
        resourcePanelPresenter = new ResourcePanelPresenter(storage, resourcePanel);
        tooltipPresenter = new TooltipPresenter(hover, tooltipView);

        storm = new StormModel(stormConfig, train);
        storm.NodeChanged += node => Debug.Log($"[Буря] узел {node}, скорость {storm.Speed:0.00}");
        stormPresenter = new StormPresenter(storm, stormView, trainView);

        journey = new JourneyModel(journeyConfig, train, storm, storage);
        journey.StateChanged += LogState;
        journeyPresenter = new JourneyPresenter(journey, trainView);
        mapScreenPresenter = new MapScreenPresenter(journey, mapScreen);
        stationScreenPresenter = new StationScreenPresenter(journey, stationScreen);
        journey.NodeReached += node => Debug.Log($"[Забег] доехали до узла {node} из {journeyConfig.nodeCount}: {NodeName(journey.Map.Current)}");
        journey.Delivery.Started += () => Debug.Log($"[Посылка] взяли {journey.Delivery.Box.Config.displayName}, везём до Грузовой на узле {journey.Delivery.DestinationColumn}. B — разобрать ящик");
        journey.Delivery.Delivered += ingots => Debug.Log($"[Посылка] доставлена! +{ingots} слитков");
        journey.Delivery.Failed += reason => Debug.Log($"[Посылка] провалена: {reason}");
        journey.Delivery.Disassembled += (machine, trait) => Debug.Log($"[Посылка] ящик разобран: в руке {machine.displayName} ({(trait != null ? trait.displayName : "без свойства")})");
        LogMap();
        LogChoices();
    }

    private void Update()
    {
        // Крафт идёт только в пути — как и работа станков.
        if (journey.State == JourneyState.Travelling) workbench.Tick(Time.deltaTime);
        journey.Tick(Time.deltaTime);
        tooltipPresenter.Tick();

        logTimer -= Time.deltaTime;
        if (logTimer <= 0f && journey.State == JourneyState.Travelling) { logTimer = 2f; LogTrain(); }

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.dKey.wasPressedThisFrame) Tail?.Damage(10);
        if (kb.rKey.wasPressedThisFrame) Tail?.Repair(10);
        if (kb.kKey.wasPressedThisFrame) locomotive.Damage(10);
        if (kb.tKey.wasPressedThisFrame)
        {
            traitIndex = traitIndex + 1 >= traits.Length ? -1 : traitIndex + 1;
            if (build.Selected != null) build.Select(build.Selected, CurrentTrait);
            Debug.Log($"[Свойство] следующий станок: {TraitName}");
        }
        if (kb.bKey.wasPressedThisFrame)
        {
            if (journey.DisassembleCargo(out var machine, out var trait)) build.Select(machine, trait);
            else Debug.Log("[Посылка] разбирать нечего");
        }
        if (kb.uKey.wasPressedThisFrame)
            Debug.Log(locomotive.UpgradeBoiler() ? $"[Коптильня] улучшена до уровня {locomotive.BoilerLevel}" : "[Коптильня] уже максимальный уровень");

        for (int i = 0; i < machines.Length && i < 9; i++)
        {
            if (kb[Key.Digit1 + i].wasPressedThisFrame)
            {
                build.Select(machines[i], CurrentTrait);
                Debug.Log($"Выбран: {machines[i].displayName} ({TraitName})");
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
        stationScreenPresenter?.Dispose();
        mapScreenPresenter?.Dispose();
        tooltipPresenter?.Dispose();
        resourcePanelPresenter?.Dispose();
        handPresenter?.Dispose();
        workbenchPresenter?.Dispose();
        journeyPresenter?.Dispose();
        stormPresenter?.Dispose();
        trainPresenter?.Dispose();
    }

    private void AttachRandomWagon() =>
        train.Attach(new WagonModel(wagons[Random.Range(0, wagons.Length)]));
    
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
            sb.Append($"  {i + 1}. {NodeName(choices[i])}{EventsText(journey.Map.Current.EventsTo(choices[i]), " [", "]")}");
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
            sb.Append($"  {i + 1}. {offer.Config.displayName} ({condition}, дерево {preview.Wood}, лом {preview.Scrap}, вес {preview.Weight:0.#})");
        }
        if (station.Cargo != null)
            sb.Append($"   | посылка: {station.Cargo.Box.displayName} в {station.Cargo.Wagon.displayName}, " +
                      $"отвезти до узла {journey.Node + journeyConfig.deliveryDistance}, награда {station.Cargo.Box.rewardIngots} слитков");
        Debug.Log(sb.ToString());
    }

    private void LogTrain()
    {
        if (!locomotive.HasLevels) return;
        var level = locomotive.Level;
        int fuel = locomotive.Boiler != null ? locomotive.Boiler.Loaded : 0;
        Debug.Log($"[Поезд] вагонов {train.Wagons.Count}, клетки {train.Cells}, вес {train.Weight:0.#}, тяга {level.traction}, скорость {train.Speed:0.00}, " +
                  $"коптильня ур.{locomotive.BoilerLevel}, топливо {fuel}/{level.tank}, хп локомотива {locomotive.Hp} | " +
                  $"буря: скорость {storm.Speed:0.00}, до хвоста {storm.DistanceToTail(train):0.0} клеток | " +
                  $"едем к {(journey.Target != null ? NodeName(journey.Target) : "?")} (узел {journey.Node + 1}), осталось {journey.TimeLeft:0} с" +
                  (journey.Delivery.IsActive ? $" | посылка: {journey.Delivery.Box.Config.displayName}, хп {journey.Delivery.Box.Hp}, до узла {journey.Delivery.DestinationColumn}" : ""));
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
