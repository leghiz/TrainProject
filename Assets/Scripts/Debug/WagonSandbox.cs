using UnityEngine;
using UnityEngine.InputSystem;

public class WagonSandbox : MonoBehaviour
{
    [SerializeField, Expandable] private WagonConfig[] wagons;
    [SerializeField, Expandable] private MachineConfig[] machines;
    [SerializeField] private WagonView wagonPrefab;
    [SerializeField, Expandable] private LocomotiveConfig locomotiveConfig;
    [SerializeField] private LocomotiveView locomotivePrefab;

    private readonly BuildModel build = new BuildModel();
    private readonly ResourceStorageModel storage = new ResourceStorageModel();

    private WagonModel model;
    private WagonView view;
    private WagonPresenter presenter;

    private LocomotiveModel locomotive;
    private LocomotivePresenter locomotivePresenter;

    private TrainModel train;
    private float logTimer;

    private void Start()
    {
        for (int i = 0; i < machines.Length; i++) Debug.Log($"{i + 1} — {machines[i].displayName}");
        storage.Changed += LogStorage;
        SpawnLocomotive();
        Spawn();
    }

    private void Update()
    {
        train?.Tick(Time.deltaTime, storage);

        logTimer -= Time.deltaTime;
        if (logTimer <= 0f) { logTimer = 2f; LogTrain(); }

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.dKey.wasPressedThisFrame) model.Damage(10);
        if (kb.rKey.wasPressedThisFrame) model.Repair(10);
        if (kb.spaceKey.wasPressedThisFrame) Spawn();
        if (kb.lKey.wasPressedThisFrame) LoadMachines();
        if (kb.kKey.wasPressedThisFrame) locomotive.Damage(10);
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

    private void Spawn()
    {
        presenter?.Dispose();
        if (model != null)
        {
            model.ResourcesChanged -= LogWagon;
            train.Detach(model);
        }
        if (view != null) Destroy(view.gameObject);

        var config = wagons[Random.Range(0, wagons.Length)];
        model = new WagonModel(config);
        model.ResourcesChanged += LogWagon;
        train.Attach(model);
        view = Instantiate(wagonPrefab);
        presenter = new WagonPresenter(model, view, build);
        Debug.Log($"Вагон: {config.displayName}, хп {model.Hp}, дерево {model.Wood}, лом {model.Scrap}");
    }
    
    private void LoadMachines()
    {
        foreach (var machine in model.Machines) LoadMachine(machine);
        if (locomotive.Boiler != null) LoadMachine(locomotive.Boiler);
    }

    private void LoadMachine(MachineModel machine)
    {
        if (!machine.NeedsLoading) return;
        int loaded = machine.Load(storage, 4);
        Debug.Log($"[Загрузка] {machine.Config.displayName}: +{loaded} {machine.Config.resource}, в станке {machine.Loaded}");
    }

    // Пока без цепочки: локомотив просто стоит правее вагона.
    private void SpawnLocomotive()
    {
        locomotive = new LocomotiveModel(locomotiveConfig);
        locomotive.HpChanged += hp => Debug.Log($"[Локомотив] хп {hp}");
        locomotive.Destroyed += () => Debug.Log("[Локомотив] разрушен — конец игры");
        var locomotiveView = Instantiate(locomotivePrefab, new Vector3(90f, 0f, 0f), Quaternion.identity);
        locomotivePresenter = new LocomotivePresenter(locomotive, locomotiveView, build);
        train = new TrainModel(locomotive);
    }

    private void LogTrain()
    {
        if (train == null || !locomotive.HasLevels) return;
        var level = locomotive.Level;
        int fuel = locomotive.Boiler != null ? locomotive.Boiler.Loaded : 0;
        Debug.Log($"[Поезд] уровень {locomotive.BoilerLevel}, клетки {train.Cells}, тяга {level.traction}, скорость {train.Speed:0.00}, топливо {fuel}/{level.tank}, хп локомотива {locomotive.Hp}");
    }

    private void LogStorage() =>
        Debug.Log($"[Склад] дерево {storage.Get(ResourceType.Wood)}, лом {storage.Get(ResourceType.Scrap)}, слитки {storage.Get(ResourceType.Ingot)}");

    private void LogWagon() =>
        Debug.Log($"[Вагон] хп {model.Hp}, осталось дерева {model.Wood}, лома {model.Scrap}");
}
