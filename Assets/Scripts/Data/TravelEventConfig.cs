using UnityEngine;

public enum TravelEventType
{
    Rain,
    Wind,
    Fog
}

[CreateAssetMenu(fileName = "Event", menuName = "Train/Travel Event")]
public class TravelEventConfig : ScriptableObject
{
    public string displayName;

    [Tooltip("Иконка на дорожке карты (понадобится вместе с UI)")]
    public Sprite icon;

    public TravelEventType type;

    [Tooltip("Дождь: урон в секунду каждому станку в вагонах")]
    [Min(0f)] public float machineDamagePerSecond = 1f;

    [Tooltip("Ветер: во сколько раз быстрее изнашивается локомотив")]
    [Min(1f)] public float wearMultiplier = 1.5f;
}
