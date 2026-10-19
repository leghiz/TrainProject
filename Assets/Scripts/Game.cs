using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    [Header("Настройки игры (конфиги — их можно раскрыть и менять прямо здесь)")]
    [Tooltip("Локомотив: хп, уровни Коптильни, рецепты верстака, обшивка")]
    [SerializeField, Expandable] private LocomotiveConfig locomotiveConfig;
    [Tooltip("Забег: число узлов, время в пути, стартовые ресурсы, карта, посылки")]
    [SerializeField, Expandable] private JourneyConfig journeyConfig;
    [Tooltip("Буря: скорость, урон")]
    [SerializeField, Expandable] private StormConfig stormConfig;

    [Header("Старт забега")]
    [Tooltip("Из этих вагонов случайно выбирается один, с которым начинается забег")]
    [SerializeField, Expandable] private WagonConfig[] startWagonPool;

    [Header("Объекты сцены (обычно трогать не нужно)")]
    [SerializeField] private TrainView trainView;
    [SerializeField] private StormView stormView;
    [SerializeField] private HandView handView;
    [SerializeField] private TooltipView tooltipView;

    [Header("Разборка (удержание ПКМ над станком или ящиком)")]
    [Tooltip("Сколько секунд держать ПКМ, чтобы разобрать")]
    [SerializeField, Min(0f)] private float dismantleHoldSeconds = 1f;

    [Header("Интерфейс (Canvas)")]
    [SerializeField] private ResourcePanelView resourcePanel;
    [SerializeField] private WorkbenchPanelView workbenchPanel;
    [SerializeField] private MapScreenView mapScreen;
    [SerializeField] private StationScreenView stationScreen;

    [Header("Конец игры — сюда подключаются экраны поражения и победы")]
    [Tooltip("Локомотив разрушен")]
    [SerializeField] private UnityEvent onGameOver;
    [Tooltip("Доехали до Конечной — конец акта")]
    [SerializeField] private UnityEvent onActFinished;

    private readonly BuildModel build = new BuildModel();
    private readonly ResourceStorageModel storage = new ResourceStorageModel();
    private readonly HoverModel hover = new HoverModel();
    private readonly CargoSelectionModel cargoSelection = new CargoSelectionModel();

    private LocomotiveModel locomotive;
    private TrainModel train;
    private StormModel storm;
    private JourneyModel journey;
    private WorkbenchModel workbench;

    private TrainPresenter trainPresenter;
    private StormPresenter stormPresenter;
    private JourneyPresenter journeyPresenter;
    private WorkbenchPresenter workbenchPresenter;
    private HandPresenter handPresenter;
    private BoilerUpgradePresenter boilerUpgradePresenter;
    private ResourcePanelPresenter resourcePanelPresenter;
    private TooltipPresenter tooltipPresenter;
    private MapScreenPresenter mapScreenPresenter;
    private StationScreenPresenter stationScreenPresenter;
    private DisassemblePresenter disassemblePresenter;
    private DismantlePresenter dismantlePresenter;

    // Удержание ПКМ: IsActive, Progress (0..1) и событие Changed — для кружка прогресса.
    public DismantleModel Dismantle { get; private set; }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    private void Start()
    {
        Dismantle = new DismantleModel(dismantleHoldSeconds);
        locomotive = new LocomotiveModel(locomotiveConfig);
        train = new TrainModel(locomotive);
        trainPresenter = new TrainPresenter(train, trainView, build, storage, hover, cargoSelection, Dismantle);
        AttachStartWagon();

        workbench = new WorkbenchModel(locomotiveConfig.recipes, storage);
        workbenchPresenter = new WorkbenchPresenter(workbench, storage, build, trainView.Locomotive, workbenchPanel, hover);
        handPresenter = new HandPresenter(build, handView);
        dismantlePresenter = new DismantlePresenter(Dismantle, handView);
        boilerUpgradePresenter = new BoilerUpgradePresenter(locomotive, storage, trainView.Locomotive.BoilerUpgrade);
        resourcePanelPresenter = new ResourcePanelPresenter(storage, resourcePanel);

        storm = new StormModel(stormConfig, train, trainView.XToCells(stormView.transform.localPosition.x));
        stormPresenter = new StormPresenter(storm, stormView, trainView);

        journey = new JourneyModel(journeyConfig, train, storm, storage);
        journeyPresenter = new JourneyPresenter(journey, trainView);
        tooltipPresenter = new TooltipPresenter(hover, tooltipView);
        mapScreenPresenter = new MapScreenPresenter(journey, mapScreen);
        stationScreenPresenter = new StationScreenPresenter(journey, stationScreen);
        disassemblePresenter = new DisassemblePresenter(cargoSelection, journey, workbench);
        journey.StateChanged += OnStateChanged;
    }

    private void Update()
    {
        if (journey == null) return;
        
        if (journey.State == JourneyState.Travelling) workbench.Tick(Time.deltaTime);
        journey.Tick(Time.deltaTime);
        Dismantle.Tick(Time.deltaTime);
        tooltipPresenter.Tick();
    }

    private void AttachStartWagon()
    {
        if (startWagonPool == null || startWagonPool.Length == 0)
        {
            Debug.LogWarning("[Game] список Start Wagon Pool пуст — забег начнётся без вагонов");
            return;
        }
        var config = startWagonPool[Random.Range(0, startWagonPool.Length)];
        if (config != null) train.Attach(new WagonModel(config));
    }

    private void OnStateChanged(JourneyState state)
    {
        if (state == JourneyState.GameOver) onGameOver?.Invoke();
        else if (state == JourneyState.Finished) onActFinished?.Invoke();
    }

    private void OnDestroy()
    {
        if (journey != null) journey.StateChanged -= OnStateChanged;
        disassemblePresenter?.Dispose();
        dismantlePresenter?.Dispose();
        stationScreenPresenter?.Dispose();
        mapScreenPresenter?.Dispose();
        tooltipPresenter?.Dispose();
        resourcePanelPresenter?.Dispose();
        boilerUpgradePresenter?.Dispose();
        handPresenter?.Dispose();
        workbenchPresenter?.Dispose();
        journeyPresenter?.Dispose();
        stormPresenter?.Dispose();
        trainPresenter?.Dispose();
    }
}
