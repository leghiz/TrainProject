using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Кнопка из спрайтов в мире: жмётся ЛКМ по своей картинке, при нажатии чуть сжимается.
// Белая обводка при наведении — отдельным HoverOutline на этом же объекте.
public class WorldButton : MonoBehaviour
{
    [Tooltip("По этой картинке ловится клик")]
    [SerializeField] private SpriteRenderer target;
    [Tooltip("Что сжимается при нажатии. Пусто — сама кнопка")]
    [SerializeField] private Transform punch;
    [SerializeField] private float punchScale = 0.85f;
    [SerializeField] private float punchTime = 0.12f;

    [Header("Удержание")]
    [Tooltip("Держишь ЛКМ — кнопка жмётся снова и снова")]
    [SerializeField] private bool repeatWhileHeld;
    [Tooltip("Через сколько секунд удержания начинает повторять")]
    [SerializeField] private float repeatDelay = 0.35f;
    [Tooltip("Сколько секунд между повторами")]
    [SerializeField] private float repeatInterval = 0.08f;

    public event Action Clicked;

    public bool Interactable { get; set; } = true;
    public bool IsHovered { get; private set; }

    private Vector3 baseScale;
    private float punchTimer = -1f;
    private bool held;
    private float heldTime;

    private void Awake()
    {
        if (punch == null) punch = transform;
        baseScale = punch.localScale;
    }

    private void OnDisable()
    {
        held = false;
        IsHovered = false;
        punchTimer = -1f;
        if (punch != null) punch.localScale = baseScale;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        IsHovered = mouse != null && IsOver(mouse) && !PointerOverUI.Check(this);
        if (mouse == null) return;

        if (IsHovered && mouse.leftButton.wasPressedThisFrame) Press();
        else if (held && mouse.leftButton.isPressed && IsHovered && repeatWhileHeld)
        {
            heldTime += Time.unscaledDeltaTime;
            if (heldTime >= repeatDelay)
            {
                heldTime -= repeatInterval;
                Press();
            }
        }
        if (!mouse.leftButton.isPressed || !IsHovered) held = false;
    }

    private void LateUpdate()
    {
        if (punchTimer < 0f) return;
        punchTimer += Time.unscaledDeltaTime;
        float t = punchTime > 0f ? Mathf.Clamp01(punchTimer / punchTime) : 1f;
        float back = t - 1f;
        float scale = Mathf.LerpUnclamped(punchScale, 1f, 1f + 2.7f * back * back * back + 1.7f * back * back);
        punch.localScale = baseScale * scale;
        if (t >= 1f) punchTimer = -1f;
    }

    private void Press()
    {
        if (!held)
        {
            held = true;
            heldTime = 0f;
        }
        if (!Interactable) return;
        punchTimer = 0f;
        Clicked?.Invoke();
    }

    private bool IsOver(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null || target == null || !target.gameObject.activeInHierarchy) return false;
        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = target.bounds;
        float squeeze = baseScale.x != 0f ? punch.localScale.x / baseScale.x : 1f;
        if (squeeze > 0f) bounds.size /= squeeze;
        return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }
}
