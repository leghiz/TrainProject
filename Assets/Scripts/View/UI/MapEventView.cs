using UnityEngine;

// Плашка погоды над станцией на карте (дождь, туман, ветер): рамка с иконкой и стрелка вниз на здание.
// Появляется с «попом», когда наводишь на станцию.
public class MapEventView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [Tooltip("Стрелка на здание: под плашкой, а у плашки снизу станции — над ней, остриём вверх")]
    [SerializeField] private Transform arrow;
    [SerializeField] private Transform box;
    [Tooltip("Что раздувается при появлении. Пусто — сама плашка")]
    [SerializeField] private Transform pop;
    [SerializeField] private float popTime = 0.2f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 1.8f;
    [SerializeField] private float hideTime = 0.08f;

    private SpriteRenderer[] sprites;
    private Color[] colors;
    private bool visible;
    private float time;
    private float delay;
    private float fade = 1f;
    private float alpha;
    private Vector3 arrowBase;
    private Vector3 boxBase;
    private Vector3 iconBase;

    private void Awake()
    {
        if (pop == null) pop = transform;
        if (arrow != null) arrowBase = arrow.localPosition;
        if (box != null) boxBase = box.localPosition;
        if (icon != null) iconBase = icon.transform.localPosition;
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        colors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
        Apply(0f);
    }

    public void Setup(Sprite sprite)
    {
        if (icon != null) icon.sprite = sprite;
    }

    // Плашка под станцией: всё отражается вниз, стрелка смотрит вверх, а иконка остаётся как есть.
    public void SetBelow(bool below)
    {
        var flip = new Vector3(1f, below ? -1f : 1f, 1f);
        if (arrow != null)
        {
            arrow.localPosition = Vector3.Scale(arrowBase, flip);
            arrow.localRotation = Quaternion.Euler(0f, 0f, below ? 180f : 0f);
        }
        if (box != null) box.localPosition = Vector3.Scale(boxBase, flip);
        if (icon != null) icon.transform.localPosition = Vector3.Scale(iconBase, flip);
    }

    public void SetVisible(bool value, float showDelay)
    {
        if (value == visible) return;
        visible = value;
        delay = value ? showDelay : 0f;
        time = 0f;
    }

    // Плашка у края карты гаснет вместе со станцией.
    public void SetFade(float value) => fade = value;

    private void LateUpdate()
    {
        time += Time.unscaledDeltaTime;
        if (visible)
        {
            float t = popTime > 0f ? Mathf.Clamp01((time - delay) / popTime) : 1f;
            float back = t - 1f;
            float scale = t <= 0f ? 0f : Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
            pop.localScale = new Vector3(scale, scale, 1f);
            alpha = t > 0f ? 1f : 0f;
        }
        else
        {
            alpha = Mathf.MoveTowards(alpha, 0f, hideTime > 0f ? Time.unscaledDeltaTime / hideTime : 1f);
            pop.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, alpha);
        }
        Apply(alpha);
    }

    private void Apply(float alpha)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            var color = colors[i];
            color.a *= alpha * fade;
            sprites[i].color = color;
            sprites[i].enabled = color.a > 0f;
        }
    }
}
