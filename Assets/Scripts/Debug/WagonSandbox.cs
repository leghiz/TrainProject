using UnityEngine;
using UnityEngine.InputSystem;

public class WagonSandbox : MonoBehaviour
{
    [SerializeField, Expandable] private WagonConfig[] wagons;
    [SerializeField, Expandable] private MachineConfig[] machines;
    [SerializeField] private WagonView wagonPrefab;

    private readonly BuildModel build = new BuildModel();
    private readonly ResourceStorageModel storage = new ResourceStorageModel();

    private WagonModel model;
    private WagonView view;
    private WagonPresenter presenter;

    private void Start()
    {
        for (int i = 0; i < machines.Length; i++) Debug.Log($"{i + 1} — {machines[i].displayName}");
        storage.Changed += LogStorage;
        Spawn();
    }

    private void Update()
    {
        model?.Tick(Time.deltaTime, storage); 

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.dKey.wasPressedThisFrame) model.Damage(10);
        if (kb.rKey.wasPressedThisFrame) model.Repair(10);
        if (kb.spaceKey.wasPressedThisFrame) Spawn();
        if (kb.lKey.wasPressedThisFrame) LoadMachines();

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
        if (model != null) model.ResourcesChanged -= LogWagon;
        if (view != null) Destroy(view.gameObject);

        var config = wagons[Random.Range(0, wagons.Length)];
        model = new WagonModel(config);
        model.ResourcesChanged += LogWagon;
        view = Instantiate(wagonPrefab);
        presenter = new WagonPresenter(model, view, build);
        Debug.Log($"Вагон: {config.displayName}, хп {model.Hp}, дерево {model.Wood}, лом {model.Scrap}");
    }
    
    private void LoadMachines()
    {
        foreach (var machine in model.Machines)
        {
            if (!machine.NeedsLoading) continue;
            int loaded = machine.Load(storage, 4);
            Debug.Log($"[Загрузка] {machine.Config.displayName}: +{loaded} {machine.Config.resource}, в станке {machine.Loaded}");
        }
    }

    private void LogStorage() =>
        Debug.Log($"[Склад] дерево {storage.Get(ResourceType.Wood)}, лом {storage.Get(ResourceType.Scrap)}, слитки {storage.Get(ResourceType.Ingot)}");

    private void LogWagon() =>
        Debug.Log($"[Вагон] хп {model.Hp}, осталось дерева {model.Wood}, лома {model.Scrap}");
}
