using UnityEngine;

[CreateAssetMenu(fileName = "Machine", menuName = "Train/Machine")]
public class MachineConfig : ScriptableObject
{
    [Header("Внешний вид")]
    public string displayName;
    public Sprite sprite;

    [Header("Форма")]
    [Tooltip("Сколько клеток вагона занимает, например (2, 1)")]
    public Vector2Int size = Vector2Int.one;

    [Header("Крафт")]
    [Min(0)] public int craftWood;
    [Min(0)] public int craftScrap;

    [Header("Работа")]
    [Tooltip("Что станок делает при срабатывании")]
    public MachineAction action;

    [Tooltip("Раз в сколько секунд срабатывает")]
    [Min(0.1f)] public float workInterval = 3f;

    [Tooltip("Extract: что добывает из вагона. Convert/Fuel: что в него загружают")]
    public ResourceType resource = ResourceType.Wood;

    [Tooltip("Extract: сколько добывает. Repair: сколько хп чинит. Convert/Fuel: сколько тратит из загруженного за раз")]
    [Min(1)] public int amount = 1;

    [Tooltip("Extract: сколько хп снимает с вагона за раз")]
    [Min(0)] public float wagonDamage = 2f;

    [Tooltip("Convert: что выдаёт на склад")]
    public ResourceType outputResource = ResourceType.Ingot;

    [Tooltip("Convert: сколько выдаёт за раз")]
    [Min(1)] public int outputAmount = 1;

    [Tooltip("Repair: сколько всего хп станок может отдать. Отдал всё — исчезает. 0 = без ограничения")]
    [Min(0)] public int charge;
}

public enum MachineAction
{
    Extract,
    Repair,
    Convert,
    Fuel
}
