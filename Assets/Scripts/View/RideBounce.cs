using System.Collections.Generic;
using UnityEngine;

// Покачивание в пути. Трясётся только сам вагон и то, что в нём стоит (станки, ящики),
// а интерфейс на вагоне (сетка, иконки, кнопки) и точки, к которым цепляются тултипы и меню, стоят на месте.
[DefaultExecutionOrder(-500)]
public class RideBounce : MonoBehaviour
{
    [Tooltip("Что трясётся, кроме станков и ящиков (они трясутся сами): обычно только Body. Интерфейс сюда не класть")]
    [SerializeField] private Transform[] shaken;

    [Header("Стыки рельсов")]
    [Tooltip("Расстояние между стыками рельсов, пиксели")]
    [SerializeField] private float jointSpacing = 64f;
    [Tooltip("Колёсные пары: насколько от края вагона внутрь, доля половины ширины")]
    [Range(0f, 1f)] [SerializeField] private float wheelInset = 0.7f;
    [Tooltip("Высота подскока на стыке, пиксели")]
    [SerializeField] private float jumpHeight = 1f;
    [Tooltip("Сколько пикселей пути длится подскок")]
    [SerializeField] private float jumpLength = 8f;

    [Header("Сцепка")]
    [Tooltip("Люфт сцепки: насколько вагон гуляет вперёд-назад, пиксели")]
    [SerializeField] private float sway = 1f;
    [Tooltip("Сколько раз в секунду гуляет сцепка на скорости 1.0")]
    [SerializeField] private float swayFrequency = 0.6f;

    private readonly List<Transform> moved = new List<Transform>();
    private readonly List<Vector3> movedTo = new List<Vector3>();
    private Vector3 applied;
    private float swayTime;
    private float swayPhase;
    private float halfWidth = 16f;

    private void Start()
    {
        swayPhase = Random.value * Mathf.PI * 2f;
        var wagon = GetComponent<WagonView>();
        var locomotive = GetComponent<LocomotiveView>();
        if (wagon != null) halfWidth = wagon.Width / 2f;
        else if (locomotive != null) halfWidth = locomotive.Width / 2f;
    }

    private void Update() => Restore();

    private void OnDisable() => Restore();

    private void LateUpdate()
    {
        var motion = TrainMotionView.Current;
        if (motion == null || !motion.IsMoving) return;

        swayTime += Time.deltaTime * swayFrequency * motion.SpeedShare;
        float x = Mathf.Round(sway * Mathf.Sin(swayTime * Mathf.PI * 2f + swayPhase));

        float wheel = halfWidth * wheelInset;
        float center = transform.position.x;
        float y = Mathf.Max(Jump(center + wheel, motion.Distance), Jump(center - wheel, motion.Distance));

        var offset = new Vector3(x, y, 0f);
        if (offset == Vector3.zero) return;

        applied = offset;
        if (shaken != null)
            foreach (var part in shaken) Shake(part);
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.GetComponent<MachineView>() != null || child.GetComponent<CargoBoxView>() != null) Shake(child);
        }
    }

    private void Shake(Transform part)
    {
        if (part == null || moved.Contains(part)) return;
        part.localPosition += applied;
        moved.Add(part);
        movedTo.Add(part.localPosition);
    }

    private void Restore()
    {
        for (int i = 0; i < moved.Count; i++)
            if (moved[i] != null && moved[i].localPosition == movedTo[i]) moved[i].localPosition -= applied;
        moved.Clear();
        movedTo.Clear();
        applied = Vector3.zero;
    }

    private float Jump(float wheelX, float distance)
    {
        float phase = Mathf.Repeat(wheelX + distance, Mathf.Max(jointSpacing, 1f));
        if (phase >= jumpLength) return 0f;
        return Mathf.Round(jumpHeight * (1f - phase / Mathf.Max(jumpLength, 0.01f)) + 0.49f);
    }
}
