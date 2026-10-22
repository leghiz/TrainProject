using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Награда на экране станции (станок или слиток): падает сверху, при наведении приподнимается, клик — забрать.
// Слиток после клика летит в счётчик слитков.
public class RewardItemView : MonoBehaviour
{
    [Tooltip("Картинка награды: по ней наводятся и жмут")]
    [SerializeField] private SpriteRenderer icon;
    [Tooltip("Что приподнимается при наведении")]
    [SerializeField] private Transform lift;
    [Tooltip("Насколько приподнимается при наведении, пиксели")]
    [SerializeField] private float hoverLift = 3f;
    [Tooltip("Плавность подъёма: чем больше, тем быстрее")]
    [SerializeField] private float liftSpeed = 18f;

    [Header("Падение сверху")]
    [Tooltip("С какой высоты падает, пиксели")]
    [SerializeField] private float dropHeight = 120f;
    [SerializeField] private float dropTime = 0.4f;
    [Tooltip("Пружинка в конце падения: насколько проскакивает вниз")]
    [SerializeField] private float overshoot = 1.4f;

    [Header("Полёт в счётчик")]
    [SerializeField] private float flyTime = 0.45f;
    [Tooltip("Во сколько раз уменьшается к концу полёта")]
    [SerializeField] private float flyEndScale = 0.5f;

    public event Action Clicked;
    public event Action Arrived;

    private Vector3 home;
    private Vector3 baseScale;
    private float time = -1f;
    private float delay;
    private bool interactive;
    private bool hovered;
    private float liftY;
    private float flying = -1f;
    private Vector3 flyFrom;
    private Vector3 flyTo;
    private bool scaleSaved;

    private void SaveScale()
    {
        if (scaleSaved) return;
        baseScale = transform.localScale;
        scaleSaved = true;
    }

    // Упасть сверху на своё место через delay секунд.
    public void Drop(Sprite sprite, Material material, Vector3 localHome, float delaySeconds)
    {
        Setup(sprite, material, localHome);
        delay = delaySeconds;
        time = 0f;
        interactive = false;
        transform.localPosition = home + Vector3.up * dropHeight;
    }

    // Сразу стоять на месте (экран открыли снова, а награда ещё не забрана).
    public void Place(Sprite sprite, Material material, Vector3 localHome)
    {
        Setup(sprite, material, localHome);
        time = -1f;
        interactive = true;
        transform.localPosition = home;
    }

    private void Setup(Sprite sprite, Material material, Vector3 localHome)
    {
        SaveScale();
        gameObject.SetActive(true);
        if (sprite != null) icon.sprite = sprite;
        if (material != null) icon.sharedMaterial = material;
        home = new Vector3(Mathf.Round(localHome.x), Mathf.Round(localHome.y), localHome.z);
        transform.localScale = baseScale;
        flying = -1f;
        hovered = false;
        liftY = 0f;
        if (lift != null) lift.localPosition = Vector3.zero;
    }

    public void Hide()
    {
        hovered = false;
        flying = -1f;
        gameObject.SetActive(false);
    }

    // Полететь к точке в мире (счётчик слитков) и там исчезнуть.
    public void FlyTo(Vector3 worldTarget)
    {
        interactive = false;
        hovered = false;
        flying = 0f;
        flyFrom = transform.position;
        flyTo = worldTarget;
    }

    private void Update()
    {
        if (!interactive || flying >= 0f) return;
        var mouse = Mouse.current;
        hovered = mouse != null && IsOver(mouse) && !PointerOverUI.Check(this);
        if (hovered && mouse.leftButton.wasPressedThisFrame) Clicked?.Invoke();
    }

    private void LateUpdate()
    {
        if (flying >= 0f)
        {
            Fly();
            return;
        }
        if (time >= 0f) Fall();

        float step = 1f - Mathf.Exp(-liftSpeed * Time.unscaledDeltaTime);
        liftY = Mathf.Lerp(liftY, hovered ? hoverLift : 0f, step);
        if (lift != null) lift.localPosition = new Vector3(0f, Mathf.Round(liftY), 0f);
    }

    private void Fall()
    {
        time += Time.unscaledDeltaTime;
        float t = dropTime > 0f ? Mathf.Clamp01((time - delay) / dropTime) : time >= delay ? 1f : 0f;
        float back = t - 1f;
        float eased = 1f + (overshoot + 1f) * back * back * back + overshoot * back * back;
        transform.localPosition = home + Vector3.up * Mathf.Round(dropHeight * (1f - eased));
        if (t < 1f) return;
        time = -1f;
        transform.localPosition = home;
        interactive = true;
    }

    private void Fly()
    {
        flying += Time.unscaledDeltaTime;
        float t = flyTime > 0f ? Mathf.Clamp01(flying / flyTime) : 1f;
        float eased = t * t * (3f - 2f * t);
        var position = Vector3.Lerp(flyFrom, flyTo, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 20f;
        transform.position = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), transform.position.z);
        transform.localScale = baseScale * Mathf.Lerp(1f, flyEndScale, eased);
        if (t < 1f) return;
        flying = -1f;
        gameObject.SetActive(false);
        Arrived?.Invoke();
    }

    private bool IsOver(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null || icon.sprite == null) return false;
        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = icon.bounds;
        return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y + hoverLift;
    }
}
