using TMPro;
using UnityEngine;

public class WagonTooltipView : MonoBehaviour
{
    [Tooltip("Всё содержимое тултипа: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Точка внизу по центру, вокруг неё тултип раздувается при появлении")]
    [SerializeField] private Transform pop;

    [SerializeField] private TMP_Text condition;
    [SerializeField] private TMP_Text scrapCount;
    [SerializeField] private TMP_Text woodCount;

    [Header("Состояние вагона (под вагоном)")]
    [Tooltip("Панель со стрелкой: ставится под нижний край вагона, прячется у вагона без эффектов")]
    [SerializeField] private Transform conditionPanel;
    [SerializeField] private TMP_Text conditionName;
    [SerializeField] private TMP_Text conditionInfo;
    [Tooltip("Расстояние от точки тултипа до верхнего края вагона, пиксели (Tooltip Offset у вагона)")]
    [SerializeField] private float anchorAboveWagon = 4f;

    [Header("Хп")]
    [Tooltip("Точка у левого края полоски: её масштаб по X = доля хп")]
    [SerializeField] private Transform hpFill;
    [Tooltip("Ширина полоски в пикселях: доля хп округляется до целого пикселя")]
    [SerializeField] private int hpWidth = 32;

    [Header("Появление")]
    [Tooltip("Насколько выше точки над вагоном, в пикселях")]
    [SerializeField] private float lift = 10f;
    [Tooltip("Сколько длится появление, секунды")]
    [SerializeField] private float showTime = 0.2f;
    [Tooltip("Сколько длится исчезновение, секунды")]
    [SerializeField] private float hideTime = 0.1f;
    [Tooltip("С какого размера начинает раздуваться")]
    [SerializeField] private float startScale = 0.5f;
    [Tooltip("Пружинка: насколько размер перелетает единицу перед тем, как встать")]
    [SerializeField] private float overshoot = 1.7f;
    [Tooltip("На сколько пикселей выезжает снизу при появлении")]
    [SerializeField] private float rise = 6f;

    private Renderer[] renderers;
    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private TMP_Text[] texts;
    private bool visible;
    private float progress;

    private void Awake()
    {
        renderers = root.GetComponentsInChildren<Renderer>(true);
        sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;
        texts = root.GetComponentsInChildren<TMP_Text>(true);
        root.SetActive(false);
    }

    public void Show(TooltipContent content, Vector3 anchor)
    {
        if (!visible)
        {
            visible = true;
            root.SetActive(true);
            Animate();
        }

        if (condition != null) condition.text = content.Condition;
        if (scrapCount != null) scrapCount.text = content.Scrap.ToString();
        if (woodCount != null) woodCount.text = content.Wood.ToString();
        if (hpFill != null)
        {
            float share = content.HasHp ? Mathf.Clamp01((float)content.Hp / content.MaxHp) : 1f;
            int width = Mathf.Max(hpWidth, 1);
            hpFill.localScale = new Vector3(Mathf.Round(share * width) / width, 1f, 1f);
        }

        transform.position = new Vector3(Mathf.Round(anchor.x), Mathf.Round(anchor.y + lift), 0f);
        KeepOnScreen();

        if (conditionPanel != null)
        {
            bool hasEffects = !string.IsNullOrEmpty(content.ConditionInfo);
            conditionPanel.gameObject.SetActive(hasEffects);
            if (hasEffects)
            {
                if (conditionName != null) conditionName.text = content.Condition;
                if (conditionInfo != null) conditionInfo.text = content.ConditionInfo;
                float bottom = anchor.y - anchorAboveWagon - content.Size.y;
                var chain = Vector3.zero;
                for (var parent = conditionPanel.parent; parent != null && parent != pop && parent != transform; parent = parent.parent)
                    chain += parent.localPosition;
                var target = new Vector3(Mathf.Round(anchor.x) - transform.position.x, Mathf.Round(bottom) - transform.position.y, 0f);
                conditionPanel.localPosition = target - chain;
            }
        }
    }

    public void Hide()
    {
        visible = false;
        if (progress <= 0f) root.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!root.activeSelf) return;

        float duration = visible ? showTime : hideTime;
        float step = duration > 0f ? Time.unscaledDeltaTime / duration : 1f;
        progress = Mathf.Clamp01(progress + (visible ? step : -step));
        Animate();

        if (!visible && progress <= 0f) root.SetActive(false);
    }

    private void Animate()
    {
        float scale;
        float alpha;
        float offset;
        if (visible)
        {
            float back = progress - 1f;
            scale = Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
            alpha = Mathf.Clamp01(progress * 4f);
            offset = Mathf.Round(rise * back * back * back);
        }
        else
        {
            scale = Mathf.Lerp(0.85f, 1f, progress);
            alpha = progress;
            offset = 0f;
        }

        if (pop != null)
        {
            pop.localScale = new Vector3(scale, scale, 1f);
            pop.localPosition = new Vector3(0f, offset, 0f);
        }
        for (int i = 0; i < sprites.Length; i++)
        {
            var color = spriteColors[i];
            color.a *= alpha;
            sprites[i].color = color;
        }
        foreach (var text in texts) text.alpha = alpha;
    }

    private void KeepOnScreen()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 popScale = Vector3.one, popPosition = Vector3.zero;
        if (pop != null)
        {
            popScale = pop.localScale;
            popPosition = pop.localPosition;
            pop.localScale = Vector3.one;
            pop.localPosition = Vector3.zero;
        }

        if (renderers != null && renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 center = cam.transform.position;

            float dx = 0f, dy = 0f;
            if (bounds.min.x < center.x - halfWidth) dx = center.x - halfWidth - bounds.min.x;
            else if (bounds.max.x > center.x + halfWidth) dx = center.x + halfWidth - bounds.max.x;
            if (bounds.max.y > center.y + halfHeight) dy = center.y + halfHeight - bounds.max.y;
            else if (bounds.min.y < center.y - halfHeight) dy = center.y - halfHeight - bounds.min.y;

            transform.position += new Vector3(Mathf.Round(dx), Mathf.Round(dy), 0f);
        }

        if (pop != null)
        {
            pop.localScale = popScale;
            pop.localPosition = popPosition;
        }
    }
}
