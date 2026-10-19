using UnityEngine;

// Одна клетка для установки (например, место под сварку в кабине локомотива): видна, пока в руке подходящий станок,
// под мышкой загорается белым, если поставить нельзя — красным. В редакторе видна всегда, чтобы её было удобно двигать.
[RequireComponent(typeof(SpriteRenderer))]
public class SlotCellView : MonoBehaviour
{
    [Header("Цвета")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color blockedColor = new Color(1f, 0.35f, 0.3f, 0.85f);

    [Header("Анимация")]
    [Tooltip("Сколько секунд клетка проявляется и гаснет")]
    [SerializeField] private float fadeTime = 0.15f;
    [Tooltip("Сколько секунд клетка загорается под мышкой")]
    [SerializeField] private float highlightTime = 0.08f;
    [Tooltip("Насколько клетка раздувается, когда на неё наводишь")]
    [SerializeField] private float hoverPop = 1.12f;
    [Tooltip("С какого размера клетка вырастает при появлении")]
    [SerializeField] private float appearScale = 0.6f;

    private SpriteRenderer cell;
    private Vector3 baseScale;
    private bool wanted;
    private bool lit;
    private bool blocked;
    private float shown;
    private float glow;

    private void Awake()
    {
        cell = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        cell.enabled = false;
    }

    public void Show(bool value)
    {
        wanted = value;
        if (!value) lit = false;
    }

    public void Highlight(bool canPlace)
    {
        lit = wanted;
        blocked = !canPlace;
    }

    public void ClearHighlight() => lit = false;

    private void LateUpdate()
    {
        float fadeStep = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
        float litStep = highlightTime > 0f ? Time.unscaledDeltaTime / highlightTime : 1f;
        shown = Mathf.MoveTowards(shown, wanted ? 1f : 0f, fadeStep);
        glow = Mathf.MoveTowards(glow, lit ? 1f : 0f, litStep);

        cell.enabled = shown > 0f;
        if (!cell.enabled) return;

        var color = Color.Lerp(idleColor, blocked ? blockedColor : hoverColor, glow);
        color.a *= shown;
        cell.color = color;

        float back = shown - 1f;
        float appear = Mathf.LerpUnclamped(appearScale, 1f, 1f + 2.7f * back * back * back + 1.7f * back * back);
        transform.localScale = baseScale * appear * Mathf.Lerp(1f, hoverPop, glow);
    }
}
