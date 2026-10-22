using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Вагон на экране станции: стрелка над ним, при наведении обводится и приподнимается, клик — взять.
// Для посылки на вагоне сверху рисуется ящик.
public class StationOfferView : MonoBehaviour
{
    [Tooltip("Картинка вагона: по ней наводятся и жмут")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Ящик посылки на вагоне (прячется у обычных вагонов)")]
    [SerializeField] private SpriteRenderer cargo;
    [Tooltip("Что приподнимается при наведении: вагон вместе с ящиком")]
    [SerializeField] private Transform lift;
    [Tooltip("Стрелка над вагоном")]
    [SerializeField] private SpriteRenderer arrow;

    [Header("Наведение")]
    [Tooltip("Насколько вагон приподнимается, пиксели")]
    [SerializeField] private float hoverLift = 4f;
    [Tooltip("Плавность подъёма: чем больше, тем быстрее")]
    [SerializeField] private float liftSpeed = 18f;
    [Tooltip("Где тултип: сдвиг от середины верхнего края вагона, пиксели")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 4f);
    [Tooltip("Цвет вагона, который уже нельзя взять")]
    [SerializeField] private Color lockedTint = new Color(1f, 1f, 1f, 0.35f);

    [Header("Стрелка")]
    [Tooltip("От верхнего края вагона до стрелки, пиксели")]
    [SerializeField] private float arrowGap = 11.5f;
    [Tooltip("Насколько стрелка покачивается вверх-вниз, пиксели")]
    [SerializeField] private float arrowBob = 1f;
    [SerializeField] private float arrowBobSpeed = 3f;

    [Header("Ящик")]
    [Tooltip("Размер клетки вагона, пиксели")]
    [SerializeField] private float cellSize = 16f;
    [Tooltip("Высота рамы с колёсами внизу спрайта, пиксели: ящик стоит над ней")]
    [SerializeField] private float chassisHeight = 7f;

    [Header("Разлёт, когда взяли другой вагон")]
    [SerializeField] private float flyTime = 0.35f;
    [Tooltip("Насколько далеко улетает в сторону, пиксели")]
    [SerializeField] private float flyDistance = 260f;

    [Header("Появление")]
    [SerializeField] private float popTime = 0.25f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 1.8f;

    public event Action Hovered;
    public event Action Unhovered;
    public event Action Clicked;

    public bool IsHovered { get; private set; }

    // Размер вагона на экране (с учётом уменьшения ряда).
    public Vector2 Size => Vector2.Scale(body.sprite != null ? (Vector2)body.sprite.bounds.size : Vector2.zero, (Vector2)body.transform.lossyScale);

    // Точка тултипа над поднятым вагоном.
    public Vector3 TooltipPoint
    {
        get
        {
            var scale = body.transform.lossyScale;
            float top = body.sprite != null ? body.sprite.bounds.max.y * scale.y : 0f;
            var origin = body.transform.position - Vector3.up * (lift.localPosition.y - hoverLift) * scale.y;
            return origin + Vector3.up * top + (Vector3)Vector2.Scale(tooltipOffset, scale);
        }
    }

    public float Width => body.sprite != null ? body.sprite.bounds.size.x : 0f;
    public float Bottom => body.sprite != null ? body.sprite.bounds.min.y : 0f;

    private bool interactable = true;
    private float delay;
    private float time;
    private float liftY;
    private float arrowAlpha = 1f;
    private Color arrowColor;
    private float flying = -1f;
    private float flyDirection;
    private Vector3 flyFrom;
    private float flyAlpha = 1f;

    private void Awake() => arrowColor = arrow != null ? arrow.color : Color.white;

    public void Setup(Sprite sprite, Material material, float appearDelay)
    {
        body.sprite = sprite;
        if (material != null) body.sharedMaterial = material;
        if (cargo != null) cargo.gameObject.SetActive(false);
        delay = appearDelay;
        time = 0f;
        PlaceArrow();
        Animate();
    }

    // Ящик стоит в левом нижнем углу вагона, как его поставят в пути.
    public void SetCargo(Sprite sprite, Vector2Int size, Sprite blank)
    {
        if (cargo == null || body.sprite == null) return;
        cargo.gameObject.SetActive(true);
        var area = (Vector2)size * cellSize;
        var bounds = body.sprite.bounds;
        cargo.transform.localPosition = new Vector3(bounds.min.x + area.x / 2f, bounds.min.y + chassisHeight + area.y / 2f, 0f);
        if (sprite != null)
        {
            cargo.sprite = sprite;
            cargo.transform.localScale = Vector3.one;
        }
        else if (blank != null)
        {
            cargo.sprite = blank;
            var blankSize = (Vector2)blank.bounds.size;
            cargo.transform.localScale = new Vector3(area.x / blankSize.x, area.y / blankSize.y, 1f);
        }
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        var tint = value ? Color.white : lockedTint;
        body.color = tint;
        if (cargo != null) cargo.color = tint;
    }

    private void PlaceArrow()
    {
        if (arrow == null || body.sprite == null) return;
        arrow.transform.localPosition = new Vector3(0f, body.sprite.bounds.max.y + arrowGap, 0f);
    }

    // Взяли другой вагон — этот улетает в свою сторону и исчезает.
    public void FlyAway(float direction)
    {
        flying = 0f;
        flyDirection = direction < 0f ? -1f : 1f;
        flyFrom = transform.localPosition;
        flyAlpha = body.color.a;
        if (!IsHovered) return;
        IsHovered = false;
        Unhovered?.Invoke();
    }

    private void Update()
    {
        if (flying >= 0f) return;
        var mouse = Mouse.current;
        bool over = mouse != null && IsOver(mouse) && !PointerOverUI.Check(this) && time >= delay;
        if (over != IsHovered)
        {
            IsHovered = over;
            if (over) Hovered?.Invoke();
            else Unhovered?.Invoke();
        }
        if (over && interactable && mouse.leftButton.wasPressedThisFrame)
        {
            AudioView.Pickup();
            Clicked?.Invoke();
        }
    }

    private void LateUpdate()
    {
        if (flying >= 0f)
        {
            Fly();
            return;
        }
        time += Time.unscaledDeltaTime;
        Animate();
    }

    private void Fly()
    {
        flying += Time.unscaledDeltaTime;
        float t = flyTime > 0f ? Mathf.Clamp01(flying / flyTime) : 1f;
        transform.localPosition = flyFrom + Vector3.right * Mathf.Round(flyDirection * flyDistance * t * t);
        float alpha = 1f - t;
        Fade(body, alpha * flyAlpha);
        Fade(cargo, alpha * flyAlpha);
        Fade(arrow, alpha * arrowAlpha);
        if (t >= 1f) Destroy(gameObject);
    }

    private static void Fade(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        var color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private void Animate()
    {
        float t = popTime > 0f ? Mathf.Clamp01((time - delay) / popTime) : 1f;
        float back = t - 1f;
        float scale = t <= 0f ? 0f : Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
        transform.localScale = new Vector3(scale, scale, 1f);

        float step = 1f - Mathf.Exp(-liftSpeed * Time.unscaledDeltaTime);
        liftY = Mathf.Lerp(liftY, IsHovered && interactable ? hoverLift : 0f, step);
        if (lift != null) lift.localPosition = new Vector3(0f, Mathf.Round(liftY), 0f);

        if (arrow == null) return;
        arrowAlpha = Mathf.MoveTowards(arrowAlpha, IsHovered || !interactable ? 0f : 1f, Time.unscaledDeltaTime * 8f);
        var color = arrowColor;
        color.a *= arrowAlpha;
        arrow.color = color;
        PlaceArrow();
        arrow.transform.localPosition += Vector3.up * Mathf.Round(Mathf.Sin(time * arrowBobSpeed * Mathf.PI) * arrowBob);
    }

    private bool IsOver(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null || body.sprite == null) return false;
        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 local = body.transform.InverseTransformPoint(world);
        local.y += lift != null ? lift.localPosition.y : 0f;
        var bounds = body.sprite.bounds;
        return local.x >= bounds.min.x && local.x <= bounds.max.x && local.y >= bounds.min.y && local.y <= bounds.max.y + hoverLift;
    }
}
