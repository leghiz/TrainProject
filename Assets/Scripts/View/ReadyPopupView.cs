using UnityEngine;

public class ReadyPopupView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [Tooltip("Стрелка под станком: ставится под спрайт сама")]
    [SerializeField] private Transform arrow;
    [Tooltip("Зазор между станком и стрелкой, пиксели")]
    [SerializeField] private float arrowGap = 3f;

    [Header("Появление")]
    [SerializeField] private float popTime = 0.25f;
    [SerializeField] private float startScale = 0.3f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 2f;

    [Header("Покачивание")]
    [Tooltip("Насколько подпрыгивает, пиксели")]
    [SerializeField] private float bobHeight = 1f;
    [Tooltip("Сколько раз в секунду")]
    [SerializeField] private float bobSpeed = 1.5f;

    private Vector3 basePosition;
    private float time;

    private void Awake() => basePosition = transform.localPosition;

    private void OnEnable()
    {
        time = 0f;
        Animate();
    }

    private void LateUpdate()
    {
        time += Time.unscaledDeltaTime;
        Animate();
    }

    private void Animate()
    {
        float t = popTime > 0f ? Mathf.Clamp01(time / popTime) : 1f;
        float back = t - 1f;
        float scale = Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
        transform.localScale = new Vector3(scale, scale, 1f);

        float bob = t < 1f ? 0f : Mathf.Round(Mathf.Sin((time - popTime) * bobSpeed * Mathf.PI * 2f) * bobHeight);
        transform.localPosition = basePosition + new Vector3(0f, bob, 0f);

        if (arrow != null && icon != null && icon.sprite != null)
        {
            float half = icon.sprite.bounds.size.y / 2f;
            var arrowRenderer = arrow.GetComponent<SpriteRenderer>();
            float arrowHalf = arrowRenderer != null && arrowRenderer.sprite != null ? arrowRenderer.sprite.bounds.size.y / 2f : 0f;
            arrow.localPosition = new Vector3(0f, -half - arrowGap - arrowHalf, 0f);
        }
    }
}
