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

    [Tooltip("Длина локомотива в клетках (нужна буре, чтобы понять, накрыла ли она его)")]
    [Min(1)] public int length = 5;

    [Header("Станки, которые стоят в локомотиве с начала игры")]
    [Expandable] public MachineConfig boiler;
    [Expandable] public MachineConfig workbench;

    [Header("Обшивка")]
    [Tooltip("Сколько листов обшивки можно повесить на локомотив")]
    [Min(0)] public int maxPlates = 3;

    [Header("Верстак")]
    [Tooltip("Какие станки можно скрафтить")]
    [Expandable] public MachineConfig[] recipes;

    [Header("Уровни коптильни (1-й элемент — уровень 1)")]
    public BoilerLevelSettings[] boilerLevels;

    [Tooltip("Без топлива поезд едет медленнее: скорость умножается на это число. Не 0 — поезд никогда не стоит")]
    [Range(0.05f, 1f)] public float noFuelSpeed = 0.3f;

    [Tooltip("Сколько дерева в коптильне в начале игры. Больше бака не влезет")]
    [Min(0)] public int startFuel = 30;
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
