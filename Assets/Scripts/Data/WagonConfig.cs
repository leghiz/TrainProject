using UnityEngine;

// Описание одного вида вагона (справочник, во время игры не меняется).
// Создаётся в Project: ПКМ -> Create -> Train -> Wagon.
[CreateAssetMenu(fileName = "Wagon", menuName = "Train/Wagon")]
public class WagonConfig : ScriptableObject
{
    [Header("Внешний вид")]
    public string displayName;
    public Sprite sprite;

    [Header("Форма")]
    [Tooltip("Ширина и высота сетки в клетках, например (3, 2)")]
    public Vector2Int size = new Vector2Int(3, 1);

    [Tooltip("Открытая крыша: дождь наносит урон станкам внутри")]
    public bool isOpen = true;

    [Header("Параметры")]
    [Min(1)] public int maxHp = 100;

    [Tooltip("Сколько дерева можно добыть из вагона")]
    [Min(0)] public int wood;

    [Tooltip("Сколько лома можно добыть из вагона")]
    [Min(0)] public int scrap;
}
