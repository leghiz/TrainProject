using UnityEngine;

[CreateAssetMenu(fileName = "CargoBox", menuName = "Train/Cargo Box")]
public class CargoBoxConfig : ScriptableObject
{
    [Header("Внешний вид")]
    public string displayName;
    public Sprite sprite;

    [Header("Форма")]
    [Tooltip("Сколько клеток вагона занимает: (2, 1) или (2, 2)")]
    public Vector2Int size = new Vector2Int(2, 1);

    [Tooltip("Из чего ящик: деревянный или железный (добыча из ящиков — позже)")]
    public ResourceType material = ResourceType.Wood;

    [Header("Параметры")]
    [Tooltip("Вес для скорости поезда (как клетки вагона)")]
    [Min(0f)] public float weight = 2f;

    [Tooltip("Хп ящика: дождь бьёт по ящикам, при 0 ящик разрушен и посылка провалена")]
    [Min(1)] public int maxHp = 10;

    [Tooltip("Сколько слитков дают, если довезти")]
    [Min(0)] public int rewardIngots = 5;
}
