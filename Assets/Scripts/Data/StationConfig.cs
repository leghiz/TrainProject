using UnityEngine;

[CreateAssetMenu(fileName = "Station", menuName = "Train/Station")]
public class StationConfig : ScriptableObject
{
    [Header("Внешний вид")]
    public string displayName;

    [Tooltip("Иконка на карте (понадобится вместе с UI)")]
    public Sprite icon;

    [Header("Вагоны")]
    [Tooltip("Сколько вагонов предлагают на станции")]
    [Min(0)] public int offerCount = 3;

    [Tooltip("Сколько из предложенных можно взять")]
    [Min(0)] public int pickCount = 1;

    [Tooltip("Из каких вагонов выбираются предложения (без повторов)")]
    [Expandable] public WagonConfig[] wagons;

    [Tooltip("Какие состояния выпадают у предложенных вагонов и как часто. Пусто — все обычные")]
    public ConditionWeight[] conditions;

    [Header("Бонус ресурсов")]
    [Tooltip("Множитель запаса дерева у вагонов этой станции (Лесосклад)")]
    [Min(0f)] public float woodMultiplier = 1f;

    [Tooltip("Множитель запаса лома у вагонов этой станции (Металлосклад)")]
    [Min(0f)] public float scrapMultiplier = 1f;

    [Header("Посылки (Грузовая)")]
    [Tooltip("Какие ящики предлагают отвезти. Пусто — станция посылок не даёт")]
    [Expandable] public CargoBoxConfig[] cargoBoxes;

    [Tooltip("Из скольких разных ящиков игрок выбирает один (по документу — из двух)")]
    [Min(1)] public int cargoChoiceCount = 2;

    [Tooltip("Какой вагончик дают вместе с посылкой (3×1 или 2×2). Выбирается тот, куда ящик влезает")]
    [Expandable] public WagonConfig[] cargoWagons;

    [Header("События на дорожках")]
    [Tooltip("Что может случиться на пути К этой станции (Лесосклад — дождь, Грузовая — ветер)")]
    public EventChance[] eventsOnWayHere;

    [Tooltip("Что может случиться на пути ОТ этой станции (Свалка — туман)")]
    public EventChance[] eventsOnWayFromHere;

    [Header("Особое")]
    [Tooltip("Конечная: доехали — конец акта")]
    public bool isFinal;
}

[System.Serializable]
public struct EventChance
{
    [Expandable] public TravelEventConfig travelEvent;
    [Range(0f, 1f)] public float chance;
}

[System.Serializable]
public struct ConditionWeight
{
    [Expandable] public WagonCondition condition;
    [Min(0)] public int weight;
}
