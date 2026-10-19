using UnityEngine;
using UnityEngine.InputSystem;

public class HoverOutline : MonoBehaviour
{
    [Tooltip("Что обводить. Пусто — первый спрайт на объекте или в детях")]
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color color = Color.white;
    [Tooltip("Сколько секунд обводка проявляется и гаснет")]
    [SerializeField] private float fadeTime = 0.08f;

    public static bool Suppressed { get; set; }

    private SpriteRenderer ring;
    private float alpha;

    private void Awake()
    {
        if (target == null) target = GetComponentInChildren<SpriteRenderer>();
        if (target == null) return;

        var outline = new GameObject("HoverOutline");
        outline.transform.SetParent(target.transform, false);
        ring = outline.AddComponent<SpriteRenderer>();
        ring.sharedMaterial = outlineMaterial;
        ring.enabled = false;
    }

    private void OnDisable()
    {
        alpha = 0f;
        if (ring != null) ring.enabled = false;
    }

    private void LateUpdate()
    {
        if (ring == null) return;

        bool visible = target.enabled && target.gameObject.activeInHierarchy && target.sprite != null;
        bool hovered = visible && !Suppressed && IsMouseOver() && !PointerOverUI.Check();
        float step = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
        alpha = Mathf.MoveTowards(alpha, hovered ? 1f : 0f, step);

        ring.enabled = visible && alpha > 0f;
        if (!ring.enabled) return;

        ring.sprite = target.sprite;
        ring.flipX = target.flipX;
        ring.flipY = target.flipY;
        ring.sortingLayerID = target.sortingLayerID;
        ring.sortingOrder = target.sortingOrder + 1;
        var tint = color;
        tint.a *= alpha;
        ring.color = tint;
    }

    private bool IsMouseOver()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return false;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = target.bounds;
        return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }
}
