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

    private readonly BuildModel build = new BuildModel();
    private readonly ResourceStorageModel storage = new ResourceStorageModel();

    private LocomotiveModel locomotive;
    private TrainModel train;
    private TrainPresenter trainPresenter;
    private StormModel storm;
    private StormPresenter stormPresenter;
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
    }

    private void Update()
    {
        train.Tick(Time.deltaTime, storage);
        storm.Tick(Time.deltaTime, train);

        logTimer -= Time.deltaTime;
        if (logTimer <= 0f) { logTimer = 2f; LogTrain(); }

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame) AttachRandomWagon();
        if (kb.dKey.wasPressedThisFrame) Tail?.Damage(10);
        if (kb.rKey.wasPressedThisFrame) Tail?.Repair(10);
        if (kb.kKey.wasPressedThisFrame) locomotive.Damage(10);
        if (kb.lKey.wasPressedThisFrame) LoadMachines();
        if (kb.nKey.wasPressedThisFrame) storm.NextNode();
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
        stormPresenter?.Dispose();
        trainPresenter?.Dispose();
    }

    private void AttachRandomWagon() =>
        train.Attach(new WagonModel(wagons[Random.Range(0, wagons.Length)]));

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
        Debug.Log($"[Поезд] вагонов {train.Wagons.Count}, клетки {train.Cells}, тяга {level.traction}, скорость {train.Speed:0.00}, " +
                  $"коптильня ур.{locomotive.BoilerLevel}, топливо {fuel}/{level.tank}, хп локомотива {locomotive.Hp} | " +
                  $"буря: узел {storm.Node}, скорость {storm.Speed:0.00}, до хвоста {storm.DistanceToTail(train):0.0} клеток");
    }

    private void LogStorage() =>
        Debug.Log($"[Склад] дерево {storage.Get(ResourceType.Wood)}, лом {storage.Get(ResourceType.Scrap)}, слитки {storage.Get(ResourceType.Ingot)}");
}
