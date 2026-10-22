using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    [SerializeField] private TrainView train;
    [SerializeField] private StormView storm;
    [Tooltip("Пока этот объект включён (станок в руке), камеру не таскаем")]
    [SerializeField] private GameObject blockWhileActive;
    [Tooltip("Пока идёт удержание (взять станок / разобрать), камеру тоже не таскаем")]
    [SerializeField] private Game game;

    [Header("Старт")]
    [Tooltip("Насколько локомотив правее центра экрана в начале игры, пиксели")]
    [SerializeField] private float locomotiveOffset = 70f;

    [Header("Перетаскивание ЛКМ")]
    [Tooltip("Сколько пикселей экрана мышь проезжает с зажатой ЛКМ, прежде чем начнётся перетаскивание")]
    [SerializeField] private float dragThreshold = 6f;
    [Tooltip("Насколько можно уехать правее локомотива, пиксели игры")]
    [SerializeField] private float rightMargin = 186f;
    [Tooltip("Насколько можно уехать левее фронта бури, пиксели игры")]
    [SerializeField] private float leftMargin = 200f;
    [Tooltip("Насколько можно уехать левее последнего вагона, пиксели игры (если буря уже накрыла хвост)")]
    [SerializeField] private float tailMargin = 40f;
    [Tooltip("Плавность: чем больше, тем быстрее камера догоняет мышь")]
    [SerializeField] private float smoothing = 18f;

    [Header("Клавиши и край экрана")]
    [Tooltip("Скорость камеры на A / D, пиксели игры в секунду")]
    [SerializeField] private float keyPanSpeed = 220f;
    [Tooltip("Зум на W (ближе) / S (дальше): насколько за секунду")]
    [SerializeField] private float keyZoomSpeed = 0.8f;
    [Tooltip("Полоса у левого и правого края экрана, где камера едет сама, доля ширины экрана (0 — выключено)")]
    [Range(0f, 0.2f)] [SerializeField] private float edgeZone = 0.03f;
    [Tooltip("Скорость камеры, когда мышка у самого края, пиксели игры в секунду")]
    [SerializeField] private float edgePanSpeed = 220f;

    [Tooltip("Со станком в руке: на сколько пикселей от края экрана камера подвозит вагон под мышкой, чтобы он был виден целиком")]
    [SerializeField] private float heldViewMargin = 8f;

    [Header("Зум колесом")]
    [Tooltip("Насколько можно отдалить камеру: 1.35 — вид на 35% больше. Ближе, чем в начале, нельзя")]
    [SerializeField] private float maxZoomOut = 1.35f;
    [Tooltip("Шаг зума за одно деление колеса")]
    [SerializeField] private float zoomStep = 0.1f;
    [Tooltip("Плавность зума")]
    [SerializeField] private float zoomSmoothing = 10f;
    [Tooltip("Сюда положены интерфейс и погода под камерой: масштабируется вместе с зумом, чтобы на экране они не менялись")]
    [SerializeField] private Transform zoomRoot;

    // Во сколько раз камера сейчас отдалена (1 — как в начале). Курсор и кружок удержания держат по нему свой размер.
    public static float Zoom { get; private set; } = 1f;

    // На сколько пикселей камеру сейчас трясёт в пути: сетка клеток едет вместе с ней и на экране стоит ровно.
    public static Vector2 ShakeOffset { get; private set; }

    [Header("Параллакс за мышкой")]
    [Tooltip("Насколько камера уезжает к мышке, когда она у края экрана, пиксели")]
    [SerializeField] private Vector2 mouseParallax = new Vector2(6f, 3f);
    [Tooltip("Плавность: чем больше, тем быстрее камера тянется за мышкой")]
    [SerializeField] private float parallaxSmoothing = 3f;

    [Header("Прибытие на станцию")]
    [Tooltip("Насколько камера приближается к локомотиву, когда поезд доехал: 0.85 — вид на 15% меньше")]
    [SerializeField] private float arrivalZoom = 0.85f;
    [Tooltip("Плавность наезда на локомотив: чем больше, тем быстрее")]
    [SerializeField] private float arrivalSmoothing = 3f;

    [Header("Тряска в пути")]
    [Tooltip("Мелкая дрожь на скорости 1.0, пиксели")]
    [SerializeField] private float shakeAmount = 1f;
    [Tooltip("Насколько быстро дрожит")]
    [SerializeField] private float shakeSpeed = 8f;
    [Tooltip("Толчки на стыках: сколько раз в секунду в среднем на скорости 1.0")]
    [SerializeField] private float joltRate = 0.5f;
    [Tooltip("Насколько камера проседает при толчке, пиксели")]
    [SerializeField] private float joltAmount = 2f;
    [Tooltip("Сколько секунд камера возвращается после толчка")]
    [SerializeField] private float joltTime = 0.15f;

    private Camera cam;
    // Пока мышка ни разу не двигалась, её позиция — (0, 0), левый край: от края в этот момент не едем.
    private bool mouseSeen;
    private float baseSize;
    private float targetZoom = 1f;
    private float baseY;
    private Vector2 parallax;
    private float shakeTime;
    private float jolt;
    private bool placed;
    private float targetX;
    private float exactX;
    private bool pressed;
    private bool dragging;
    private Vector2 pressScreen;
    private float pressTargetX;
    private bool arriving;
    private Transform lookTarget;

    private bool Blocked =>
        (blockWhileActive != null && blockWhileActive.activeInHierarchy) ||
        (game != null && game.Dismantle != null && game.Dismantle.IsActive);

    private void Awake()
    {
        cam = GetComponent<Camera>();
        baseY = transform.position.y;
        baseSize = cam.orthographicSize;
        Zoom = 1f;
    }

    // Поезд доехал: камера плавно встаёт по центру локомотива и чуть приближается; мышь и клавиши её пока не двигают.
    public void SetArrival(bool value) => arriving = value;

    // Обучение: камера плавно едет к цели (буря позади поезда) и держит её; null — снова как обычно.
    public void LookAt(Transform target) => lookTarget = target;

    // Конец обучения: камера плавно отъезжает на самый дальний зум.
    public void ZoomOutFully() => targetZoom = Mathf.Max(maxZoomOut, 1f);

    private void UpdateZoom(Mouse mouse)
    {
        if (!arriving && mouse != null && !PointerOverUI.Check())
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(wheel, 0f)) targetZoom -= Mathf.Sign(wheel) * zoomStep;
        }
        var keyboard = Keyboard.current;
        if (!arriving && keyboard != null)
        {
            if (keyboard.wKey.isPressed) targetZoom -= keyZoomSpeed * Time.unscaledDeltaTime;
            if (keyboard.sKey.isPressed) targetZoom += keyZoomSpeed * Time.unscaledDeltaTime;
        }
        if (PointerOverUI.Modal != null) targetZoom = 1f;
        if (arriving) targetZoom = arrivalZoom;
        targetZoom = Mathf.Clamp(targetZoom, arriving ? Mathf.Min(arrivalZoom, 1f) : 1f, Mathf.Max(maxZoomOut, 1f));
        float zoomSpeed = arriving ? arrivalSmoothing : zoomSmoothing;
        Zoom = Mathf.Lerp(Zoom, targetZoom, 1f - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime));
        if (Mathf.Abs(Zoom - targetZoom) < 0.001f) Zoom = targetZoom;

        cam.orthographicSize = baseSize * Zoom;
        if (zoomRoot != null) zoomRoot.localScale = new Vector3(Zoom, Zoom, 1f);
    }

    private void PlaceAtStart()
    {
        placed = true;
        targetX = train != null && train.Locomotive != null
            ? train.Locomotive.transform.position.x - locomotiveOffset
            : transform.position.x;
        targetX = Clamp(targetX);
        exactX = targetX;
    }

    private void LateUpdate()
    {
        if (!placed) PlaceAtStart();

        var mouse = Mouse.current;
        UpdateZoom(mouse);
        if (arriving || lookTarget != null)
        {
            pressed = false;
            dragging = false;
        }
        else if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame && !Blocked && !PointerOverUI.Check())
            {
                pressed = true;
                dragging = false;
                pressScreen = mouse.position.ReadValue();
                pressTargetX = targetX;
            }

            if (!mouse.leftButton.isPressed || Blocked)
            {
                pressed = false;
                dragging = false;
            }
            else if (pressed)
            {
                float delta = mouse.position.ReadValue().x - pressScreen.x;
                if (!dragging && Mathf.Abs(delta) >= dragThreshold) dragging = true;
                if (dragging) targetX = pressTargetX - delta * WorldPerScreenPixel();
            }
        }

        if (lookTarget != null) targetX = lookTarget.position.x;
        else if (arriving && train != null && train.Locomotive != null) targetX = train.Locomotive.transform.position.x;
        else if (!dragging && PointerOverUI.Modal == null) targetX += PanSpeed(mouse) * Zoom * Time.unscaledDeltaTime;
        if (!arriving && lookTarget == null) KeepHeldTargetInView();

        targetX = Clamp(targetX);
        float follow = arriving || lookTarget != null ? arrivalSmoothing : smoothing;
        exactX = Mathf.Lerp(exactX, targetX, 1f - Mathf.Exp(-follow * Time.unscaledDeltaTime));
        UpdateParallax(mouse);
        Apply(UpdateShake());
    }

    // Станок в руке, мышка над вагоном, который виден не целиком, — камера плавно подвозит его на экран.
    private void KeepHeldTargetInView()
    {
        if (!HandView.Holding || train == null || PointerOverUI.Modal != null) return;
        var bounds = train.HeldOverBounds;
        if (!bounds.HasValue) return;

        float halfWidth = cam.orthographicSize * cam.aspect;
        var part = bounds.Value;
        float low = part.max.x + heldViewMargin - halfWidth;
        float high = part.min.x - heldViewMargin + halfWidth;
        targetX = low > high ? part.center.x : Mathf.Clamp(targetX, low, high);
    }

    // A / D и мышка у края экрана: чем ближе к краю, тем быстрее.
    private float PanSpeed(Mouse mouse)
    {
        float speed = 0f;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) speed -= keyPanSpeed;
            if (keyboard.dKey.isPressed) speed += keyPanSpeed;
        }

        if (mouse == null || edgeZone <= 0f || !Application.isFocused || PointerOverUI.OverCanvas()) return speed;
        if (!mouseSeen)
        {
            if (mouse.delta.ReadValue() == Vector2.zero) return speed;
            mouseSeen = true;
        }
        Vector2 screen = mouse.position.ReadValue();
        float width = Mathf.Max(cam.pixelWidth, 1);
        if (screen.y < 0f || screen.y > cam.pixelHeight || screen.x < 0f || screen.x > width) return speed;
        float zone = width * edgeZone;
        if (screen.x < zone) speed -= edgePanSpeed * (1f - screen.x / zone);
        else if (screen.x > width - zone) speed += edgePanSpeed * (1f - (width - screen.x) / zone);
        return speed;
    }

    private void UpdateParallax(Mouse mouse)
    {
        if (mouse == null || dragging) return;

        Vector2 screen = mouse.position.ReadValue();
        var normalized = new Vector2(
            Mathf.Clamp(screen.x / Mathf.Max(cam.pixelWidth, 1) * 2f - 1f, -1f, 1f),
            Mathf.Clamp(screen.y / Mathf.Max(cam.pixelHeight, 1) * 2f - 1f, -1f, 1f));
        var target = PointerOverUI.Modal != null || arriving ? Vector2.zero : Vector2.Scale(normalized, mouseParallax);
        parallax = Vector2.Lerp(parallax, target, 1f - Mathf.Exp(-parallaxSmoothing * Time.unscaledDeltaTime));
    }

    private Vector2 UpdateShake()
    {
        var motion = TrainMotionView.Current;
        float share = motion != null ? Mathf.Clamp01(motion.SpeedShare) : 0f;

        jolt = Mathf.MoveTowards(jolt, 0f, joltTime > 0f ? Time.deltaTime / joltTime : 1f);
        if (share <= 0f) return new Vector2(0f, -jolt * joltAmount);

        shakeTime += Time.deltaTime * shakeSpeed;
        if (Random.value < joltRate * share * Time.deltaTime) jolt = 1f;

        float x = (Mathf.PerlinNoise(shakeTime, 0.37f) - 0.5f) * shakeAmount * share;
        float y = (Mathf.PerlinNoise(0.71f, shakeTime) - 0.5f) * 2f * shakeAmount * share;
        return new Vector2(x, y - jolt * joltAmount);
    }

    private void Apply(Vector2 shake)
    {
        var position = transform.position;
        float calmX = Mathf.Round(exactX + parallax.x);
        float calmY = Mathf.Round(baseY + parallax.y);
        position.x = Mathf.Round(exactX + parallax.x + shake.x);
        position.y = Mathf.Round(baseY + parallax.y + shake.y);
        ShakeOffset = new Vector2(position.x - calmX, position.y - calmY);
        transform.position = position;
    }

    private float WorldPerScreenPixel() => cam.orthographicSize * 2f / Mathf.Max(cam.pixelHeight, 1);

    private float Clamp(float x)
    {
        if (cam == null || train == null || train.Locomotive == null) return x;

        float halfWidth = cam.orthographicSize * cam.aspect;
        var locomotive = train.Locomotive;
        float max = locomotive.transform.position.x + locomotive.Width / 2f + rightMargin - halfWidth;
        float left = train.TailLeft - tailMargin;
        if (storm != null) left = Mathf.Min(left, storm.transform.position.x - leftMargin);
        float min = left + halfWidth;
        return Mathf.Clamp(x, Mathf.Min(min, max), max);
    }
}
