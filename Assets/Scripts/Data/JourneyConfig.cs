using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Journey", menuName = "Train/Journey")]
public class JourneyConfig : ScriptableObject
{
    [Tooltip("Сколько узлов до конечной (последний узел — конечная, конец акта)")]
    [Min(1)] public int nodeCount = 12;

    [Tooltip("Сколько секунд поезд едет от узла до узла")]
    [Min(1f)] public float travelSeconds = 60f;

    [Header("Старт")]
    [Tooltip("Сколько ресурсов на складе в начале забега")]
    [Min(0)] public int startWood = 6;
    [Min(0)] public int startScrap = 4;

    [Header("Карта")]
    [Tooltip("Сколько узлов может быть в одном столбце карты")]
    [Min(1)] public int minNodesPerColumn = 2;
    [Min(1)] public int maxNodesPerColumn = 3;

    [Tooltip("Шанс, что между соседними столбцами появится съезд на соседнюю дорожку (одна лишняя дорога): 0 — только обязательные дороги, 1 — съезд в каждом столбце")]
    [Range(0f, 1f)] public float extraRoadChance = 0.5f;

    [Tooltip("Какие станции встречаются на карте и как часто (больше вес — чаще)")]
    public StationWeight[] stations;

    [Tooltip("Станция в самом конце карты")]
    [Expandable] public StationConfig finalStation;

    [Tooltip("0 — каждый раз новая карта, другое число — всегда одна и та же")]
    public int seed;

    [Header("Доставка")]
    [Tooltip("Через сколько узлов после взятия посылки стоит Грузовая-получатель")]
    [Min(1)] public int deliveryDistance = 3;

    [Tooltip("В этих колонках карты все станции — Грузовые: посылку на маршруте точно предложат (получатель — через deliveryDistance узлов)")]
    public int[] cargoColumns = { 2, 6 };

    [Tooltip("Станция-получатель (обычно та же Грузовая)")]
    [Expandable] public StationConfig deliveryStation;

    [Tooltip("Запасной список: из него выпадает случайный станок, если у ящика свой список «что внутри» пуст")]
    [Expandable] public MachineConfig[] disassemblyMachines;

    [Tooltip("С каким шансом скрафченный на верстаке станок получает случайное свойство (0..1)")]
    [Range(0f, 1f)] public float craftTraitChance = 0.3f;

    [Tooltip("Из каких свойств выпадает случайное у станков из ящика (разборка, награда за доставку и крафт)")]
    [Expandable] public MachineTrait[] disassemblyTraits;
}

[Serializable]
public struct StationWeight
{
    [Expandable] public StationConfig station;
    [Min(0)] public int weight;
}
