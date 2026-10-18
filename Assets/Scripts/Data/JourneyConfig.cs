using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Journey", menuName = "Train/Journey")]
public class JourneyConfig : ScriptableObject
{
    [Tooltip("Сколько узлов до конечной (последний узел — конечная, конец акта)")]
    [Min(1)] public int nodeCount = 12;

    [Tooltip("Сколько секунд поезд едет от узла до узла")]
    [Min(1f)] public float travelSeconds = 60f;

    [Header("Карта")]
    [Tooltip("Сколько узлов может быть в одном столбце карты")]
    [Min(1)] public int minNodesPerColumn = 2;
    [Min(1)] public int maxNodesPerColumn = 3;

    [Tooltip("Какие станции встречаются на карте и как часто (больше вес — чаще)")]
    public StationWeight[] stations;

    [Tooltip("Станция в самом конце карты")]
    [Expandable] public StationConfig finalStation;

    [Tooltip("0 — каждый раз новая карта, другое число — всегда одна и та же")]
    public int seed;
}

[Serializable]
public struct StationWeight
{
    [Expandable] public StationConfig station;
    [Min(0)] public int weight;
}
