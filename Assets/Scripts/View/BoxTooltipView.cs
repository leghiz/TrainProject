using TMPro;
using UnityEngine;

// Тултип ящика посылки: плашка над ящиком со стрелкой вниз — заголовок и подсказка.
public class BoxTooltipView : MonoBehaviour
{
    [Tooltip("Всё содержимое тултипа: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Что раздувается при появлении: вокруг кончика стрелки")]
    [SerializeField] private Transform pop;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text info;

    [Header("Появление")]
    [SerializeField] private float showTime = 0.2f;
    [SerializeField] private float hideTime = 0.1f;
    [SerializeField] private float startScale = 0.5f;
    [Tooltip("Пружинка: насколько размер перелетает единицу перед тем, как встать")]
    [SerializeField] private float overshoot = 1.7f;
    [Tooltip("На сколько пикселей выезжает снизу при появлении")]
    [SerializeField] private float rise = 6f;

    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private TMP_Text[] texts;
    private bool visible;
    private float progress;

    private void Awake()
    {
        sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;
        texts = root.GetComponentsInChildren<TMP_Text>(true);
        root.SetActive(false);
    }

    // anchor — середина верхнего края ящика: туда смотрит кончик стрелки.
    public void Show(TooltipContent content, Vector3 anchor)
    {
        if (!visible)
        {
            visible = true;
            root.SetActive(true);
            Animate();
        }
        if (title != null) title.text = content.Title;
        if (info != null) info.text = content.Info;
        transform.position = new Vector3(Mathf.Round(anchor.x), Mathf.Round(anchor.y), 0f);
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
            var color = UiPalette.For(sprites[i], spriteColors[i]);
            color.a *= alpha;
            sprites[i].color = color;
        }
        foreach (var text in texts) text.alpha = alpha;
    }
}
