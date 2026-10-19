using UnityEngine;

[DefaultExecutionOrder(-500)]
public class RideBounce : MonoBehaviour
{
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

    private Vector3 applied;
    private Vector3 appliedAt;
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

    private void Update()
    {
        if (applied != Vector3.zero && transform.localPosition == appliedAt) transform.localPosition -= applied;
        applied = Vector3.zero;
    }

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
        transform.localPosition += offset;
        applied = offset;
        appliedAt = transform.localPosition;
    }

    private float Jump(float wheelX, float distance)
    {
        float phase = Mathf.Repeat(wheelX + distance, Mathf.Max(jointSpacing, 1f));
        if (phase >= jumpLength) return 0f;
        return Mathf.Round(jumpHeight * (1f - phase / Mathf.Max(jumpLength, 0.01f)) + 0.49f);
    }

    private void OnDisable()
    {
        if (applied != Vector3.zero && transform.localPosition == appliedAt) transform.localPosition -= applied;
        applied = Vector3.zero;
    }
}
