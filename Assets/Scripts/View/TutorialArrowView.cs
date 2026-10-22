using UnityEngine;

// Обучение: белая стрелка над тем, что сейчас надо нажать, покачивается вверх-вниз.
public class TutorialArrowView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer arrow;
    [Tooltip("От верхнего края цели до кончика стрелки, пиксели")]
    [SerializeField] private float gap = 3f;
    [Tooltip("Насколько стрелка покачивается, пиксели")]
    [SerializeField] private float bob = 2f;
    [SerializeField] private float bobSpeed = 2.5f;

    private Transform target;
    private float time;

    private void Awake() => arrow.enabled = false;

    public void Point(Transform value)
    {
        target = value;
        if (value == null) arrow.enabled = false;
    }

    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy || !TryBounds(target, out var bounds))
        {
            arrow.enabled = false;
            return;
        }
        arrow.enabled = true;
        time += Time.unscaledDeltaTime;
        float half = arrow.sprite != null ? arrow.sprite.bounds.extents.y * Mathf.Abs(arrow.transform.lossyScale.y) : 2.5f;
        float y = bounds.max.y + gap + half + Mathf.Round(Mathf.Sin(time * bobSpeed * Mathf.PI) * bob);
        transform.position = new Vector3(Mathf.Round(bounds.center.x), Mathf.Round(y) + 0.5f, transform.position.z);
    }

    // Рамка всех видимых картинок цели (обводку при наведении не считаем).
    private static bool TryBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var sprite in target.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!sprite.enabled || sprite.sprite == null || sprite.name == "HoverOutline") continue;
            if (any) bounds.Encapsulate(sprite.bounds);
            else bounds = sprite.bounds;
            any = true;
        }
        return any;
    }
}
