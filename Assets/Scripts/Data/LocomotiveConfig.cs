using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Locomotive", menuName = "Train/Locomotive")]
public class LocomotiveConfig : ScriptableObject
{
    [Header("Внешний вид")]
    public string displayName;
    public Sprite sprite;

    [Header("Параметры")]
    [Min(1)] public int maxHp = 200;

    [Header("Станки, которые стоят в локомотиве с начала игры")]
    [Expandable] public MachineConfig boiler;
    [Expandable] public MachineConfig workbench;

    [Header("Уровни коптильни (1-й элемент — уровень 1)")]
    public BoilerLevelSettings[] boilerLevels;

    [Tooltip("Без топлива поезд едет медленнее: скорость умножается на это число. Не 0 — поезд никогда не стоит")]
    [Range(0.05f, 1f)] public float noFuelSpeed = 0.3f;
}

[Serializable]
public struct BoilerLevelSettings
{
    [Tooltip("Бак: сколько дерева влезает в коптильню")]
    public int tank;

    [Tooltip("Скорость поезда на этом уровне")]
    public float speed;

    [Tooltip("Износ локомотива в пути, хп в секунду")]
    public float wearPerSecond;

    [Tooltip("Тяга: сколько клеток вагонов поезд тянет без потери скорости")]
    public int traction;

    [Tooltip("Расход топлива: 1 дерево раз в столько секунд")]
    public float fuelInterval;
}
