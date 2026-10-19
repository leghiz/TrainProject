using UnityEngine;

[CreateAssetMenu(fileName = "Condition", menuName = "Train/Wagon Condition")]
public class WagonCondition : ScriptableObject
{
    public string displayName;

    [Tooltip("Множитель запаса дерева и лома (Ржавый 0.5, Новый 2, Призрачный 0.25)")]
    [Min(0f)] public float resourceMultiplier = 1f;

    [Tooltip("Множитель веса для скорости поезда (Ржавый 0.5, Призрачный 0)")]
    [Min(0f)] public float weightMultiplier = 1f;
}
