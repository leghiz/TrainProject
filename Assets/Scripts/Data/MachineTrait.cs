using UnityEngine;

[CreateAssetMenu(fileName = "Trait", menuName = "Train/Machine Trait")]
public class MachineTrait : ScriptableObject
{
    public string displayName;

    [Tooltip("Во сколько раз быстрее работает (Безумный — 2)")]
    [Min(0.01f)] public float speedMultiplier = 1f;

    [Tooltip("Множитель затрат при работе: урон вагону при добыче, сколько берёт из загруженного (Аккуратный — 0.5)")]
    [Min(0f)] public float costMultiplier = 1f;

    [Tooltip("Не получает урон от дождя (Водостойкий)")]
    public bool rainproof;

    [Tooltip("Материал с шейдером свойства (прыжки, синий отлив, блики). Пусто — обычный спрайт")]
    public Material material;

    [Tooltip("Материал названия в тултипе: цвет и переливание. Пусто — обычный текст")]
    public Material nameMaterial;
}
