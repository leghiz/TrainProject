using UnityEngine;

[CreateAssetMenu(fileName = "Storm", menuName = "Train/Storm")]
public class StormConfig : ScriptableObject
{
    [Header("Скорость")]
    [Tooltip("Скорость бури на узлах: 1-й элемент — 1-й узел")]
    public float[] speedByNode = { 0.9f, 0.95f, 1.05f, 1.1f };

    [Tooltip("Когда список кончился, скорость растёт на столько за каждый следующий узел")]
    public float speedStepAfterList = 0.05f;

    [Tooltip("Сколько клеток в секунду проходит скорость 1.0")]
    [Min(0.01f)] public float cellsPerSecondAtSpeed1 = 1f;

    [Header("Погоня")]
    [Tooltip("Расстояние от бури до хвоста поезда в начале игры, в клетках")]
    [Min(0f)] public float startDistance = 10f;

    [Tooltip("Урон в секунду каждому накрытому вагону и локомотиву")]
    [Min(0f)] public float damagePerSecond = 5f;
}
