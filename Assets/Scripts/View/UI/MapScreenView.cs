using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Карта маршрута. Станции стоят по сетке: соседние соединяются прямой дорогой (Line) или диагональной под 45° (DiagonalLine).
// Целиком маршрут на экран не влезает, поэтому видно столько столбцов, сколько помещается: текущая станция слева,
// всё, что за окном, спрятано. Колесо, A / D или перетаскивание ЛКМ листают карту вперёд-назад.
// Станции и дороги чуть едут за мышкой поверх фона. Лежит под камерой (ZoomRoot); пока открыта, поезд под ней не наводится.
public class MapScreenView : MonoBehaviour
{
    [Tooltip("Всё содержимое карты: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Слой со станциями и дорогами: прокручивается и едет за мышкой")]
    [SerializeField] private Transform content;

    [Header("Образцы (выключены, код делает из них копии)")]
    [SerializeField] private MapNodeView nodeTemplate;
    [Tooltip("Прямая дорога между соседними станциями в одном ряду")]
    [SerializeField] private SpriteRenderer lineTemplate;
    [Tooltip("Диагональная дорога: нарисована сверху-слева вниз-направо, обратная — она же, отражённая")]
    [SerializeField] private SpriteRenderer diagonalTemplate;

    [Header("Сетка")]
    [Tooltip("Шаг сетки по горизонтали и вертикали, пиксели. Одинаковый, чтобы диагональ шла ровно под 45°")]
    [SerializeField] private float spacing = 52f;

    [Header("Окно")]
    [Tooltip("Ширина видимой части карты, пиксели")]
    [SerializeField] private float windowWidth = 300f;
    [Tooltip("Высота видимой части карты, пиксели")]
    [SerializeField] private float windowHeight = 150f;
    [Tooltip("Отступ крайних станций от краёв окна, пиксели")]
    [SerializeField] private float margin = 20f;
    [Tooltip("На сколько пикселей у краёв окна дороги, уходящие за край, гаснут")]
    [SerializeField] private float fadeWidth = 10f;
    [Tooltip("Как быстро карта доезжает до нужного места")]
    [SerializeField] private float scrollSpeed = 6f;

    [Header("Листать")]
    [Tooltip("На сколько столбцов листает одно деление колеса")]
    [SerializeField] private float wheelStep = 1f;
    [Tooltip("Сколько столбцов в секунду листают A / D")]
    [SerializeField] private float keySpeed = 4f;

    [Header("За мышкой")]
    [Tooltip("Насколько станции съезжают к мышке у края экрана, пиксели")]
    [SerializeField] private Vector2 parallax = new Vector2(6f, 4f);
    [Tooltip("Фон карты: едет за мышкой меньше станций, отсюда глубина")]
    [SerializeField] private Transform background;
    [Tooltip("Насколько съезжает фон, пиксели")]
    [SerializeField] private Vector2 backgroundParallax = new Vector2(3f, 2f);
    [Tooltip("Плавность: чем меньше, тем мягче догоняет мышку")]
    [SerializeField] private float parallaxSpeed = 3f;

    [Header("Появление")]
    [Tooltip("Сколько секунд карта проявляется и тает: игра под ней видна, пока она тает")]
    [SerializeField] private float appearTime = 0.35f;
    [Tooltip("Насколько карта крупнее в начале появления (и в конце исчезновения)")]
    [SerializeField] private float appearZoom = 0.04f;

    [Header("Мы здесь")]
    [Tooltip("Стрелка над текущей станцией")]
    [SerializeField] private Transform currentMarker;
    [Tooltip("От верхнего края здания до стрелки, пиксели")]
    [SerializeField] private float markerGap = 3f;
    [SerializeField] private float markerBob = 1f;
    [SerializeField] private float markerBobSpeed = 1.5f;

    [Header("Иконки без своих картинок")]
    [Tooltip("Старт забега: у него нет станции")]
    [SerializeField] private Sprite startIcon;
    [Tooltip("Станция без иконки в конфиге")]
    [SerializeField] private Sprite unknownIcon;

    public Sprite StartIcon => startIcon;
    public Sprite UnknownIcon => unknownIcon;
    public bool IsOpen => root.activeSelf;

    private readonly List<(MapNodeView view, Vector2Int cell)> nodes = new List<(MapNodeView, Vector2Int)>();
    private readonly List<SpriteRenderer> roads = new List<SpriteRenderer>();
    private MapNodeView current;
    private Vector2Int currentCell;
    private int lastColumn;
    private float firstColumn;
    private float firstColumnTarget;
    private Vector2 scroll;
    private Vector2 mouseShift;
    private Vector3 backgroundBase;
    private bool scrollPlaced;
    private bool dragging;
    private float dragStartX;
    private float dragStartColumn;
    private float time;
    private bool shown;
    private float appear;
    private SpriteRenderer backgroundSprite;
    private Color backgroundColor;
    private SpriteRenderer markerSprite;
    private Color markerColor;

    // Сколько столбцов целиком помещается в окно.
    private int VisibleColumns => Mathf.Max(1, Mathf.FloorToInt((windowWidth - margin * 2f) / spacing) + 1);
    private float MaxFirstColumn => Mathf.Max(0f, lastColumn - VisibleColumns + 1);

    private void Awake()
    {
        if (nodeTemplate != null) nodeTemplate.gameObject.SetActive(false);
        if (lineTemplate != null) lineTemplate.gameObject.SetActive(false);
        if (diagonalTemplate != null) diagonalTemplate.gameObject.SetActive(false);
        if (background != null)
        {
            backgroundBase = background.localPosition;
            backgroundSprite = background.GetComponent<SpriteRenderer>();
            if (backgroundSprite != null) backgroundColor = backgroundSprite.color;
        }
        if (currentMarker != null)
        {
            markerSprite = currentMarker.GetComponent<SpriteRenderer>();
            if (markerSprite != null) markerColor = markerSprite.color;
        }
    }

    // Карта не выскакивает, а проявляется поверх игры; при отъезде так же тает.
    public void Show()
    {
        if (!root.activeSelf)
        {
            firstColumnTarget = Mathf.Clamp(currentCell.x, 0f, MaxFirstColumn);
            appear = 0f;
        }
        shown = true;
        root.SetActive(true);
        PointerOverUI.Modal = transform;
    }

    public void Hide()
    {
        shown = false;
        dragging = false;
        if (PointerOverUI.Modal == transform) PointerOverUI.Modal = null;
    }

    private void OnDestroy()
    {
        if (PointerOverUI.Modal == transform) PointerOverUI.Modal = null;
    }

    // cell.x — столбец (шаг маршрута), cell.y — ряд (вниз от середины — плюс).
    public MapNodeView AddNode(Vector2Int cell)
    {
        var node = Instantiate(nodeTemplate, content);
        node.gameObject.SetActive(true);
        node.transform.localPosition = CellToLocal(cell);
        nodes.Add((node, cell));
        lastColumn = Mathf.Max(lastColumn, cell.x);
        return node;
    }

    // Дорога между соседними столбцами: ряд тот же — прямая, на ряд выше или ниже — диагональ.
    public void AddRoad(Vector2Int from, Vector2Int to)
    {
        bool straight = from.y == to.y;
        var template = straight ? lineTemplate : diagonalTemplate;
        if (template == null) return;
        var road = Instantiate(template, content);
        road.gameObject.SetActive(true);
        road.transform.localPosition = (CellToLocal(from) + CellToLocal(to)) / 2f;
        if (!straight) road.flipX = to.y < from.y == to.x > from.x;
        road.transform.SetAsFirstSibling();
        roads.Add(road);
    }

    public void SetCurrent(MapNodeView node)
    {
        current = node;
        foreach (var entry in nodes)
            if (entry.view == node) currentCell = entry.cell;
        firstColumnTarget = Mathf.Clamp(currentCell.x, 0f, MaxFirstColumn);
    }

    private Vector3 CellToLocal(Vector2Int cell) => new Vector3(cell.x * spacing, -cell.y * spacing, 0f);

    private void LateUpdate()
    {
        if (!root.activeSelf || content == null) return;
        time += Time.unscaledDeltaTime;

        appear = Mathf.MoveTowards(appear, shown ? 1f : 0f, appearTime > 0f ? Time.unscaledDeltaTime / appearTime : 1f);
        if (!shown && appear <= 0f)
        {
            root.SetActive(false);
            return;
        }
        float visibility = Mathf.SmoothStep(0f, 1f, appear);
        float zoom = 1f + (1f - visibility) * appearZoom;
        root.transform.localScale = new Vector3(zoom, zoom, 1f);
        if (backgroundSprite != null)
        {
            var color = backgroundColor;
            color.a *= visibility;
            backgroundSprite.color = color;
        }

        if (shown) ReadScrollInput();
        firstColumnTarget = Mathf.Clamp(firstColumnTarget, 0f, MaxFirstColumn);
        if (!scrollPlaced) firstColumn = firstColumnTarget;
        firstColumn = Mathf.Lerp(firstColumn, firstColumnTarget, 1f - Mathf.Exp(-scrollSpeed * Time.unscaledDeltaTime));

        var target = new Vector2(-windowWidth / 2f + margin - firstColumn * spacing, CenterRow() * spacing);
        if (!scrollPlaced)
        {
            scroll = target;
            scrollPlaced = true;
        }
        scroll = Vector2.Lerp(scroll, target, 1f - Mathf.Exp(-scrollSpeed * Time.unscaledDeltaTime));
        // Без округления до пикселя: при плавном сдвиге за мышкой станции не дёргаются по ступенькам.
        mouseShift = Vector2.Lerp(mouseShift, MouseTarget(), 1f - Mathf.Exp(-parallaxSpeed * Time.unscaledDeltaTime));
        var offset = scroll + Vector2.Scale(mouseShift, parallax);
        content.localPosition = new Vector3(offset.x, offset.y, 0f);
        if (background != null) background.localPosition = backgroundBase + (Vector3)Vector2.Scale(mouseShift, backgroundParallax);

        foreach (var entry in nodes) entry.view.SetFade(Fade(entry.view.transform.localPosition, fadeWidth) * visibility);
        foreach (var road in roads)
        {
            var color = road.color;
            color.a = Fade(road.transform.localPosition, fadeWidth) * visibility;
            road.color = color;
        }
        PlaceMarker(visibility);
    }

    // Середина рядов у столбцов, что сейчас в окне: так видны и верхние, и нижние станции.
    private float CenterRow()
    {
        int from = Mathf.FloorToInt(firstColumnTarget);
        int to = from + VisibleColumns - 1;
        int min = int.MaxValue, max = int.MinValue;
        foreach (var entry in nodes)
        {
            if (entry.cell.x < from || entry.cell.x > to) continue;
            min = Mathf.Min(min, entry.cell.y);
            max = Mathf.Max(max, entry.cell.y);
        }
        return min > max ? currentCell.y : (min + max) / 2f;
    }

    private void ReadScrollInput()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) firstColumnTarget -= keySpeed * Time.unscaledDeltaTime;
            if (keyboard.dKey.isPressed) firstColumnTarget += keySpeed * Time.unscaledDeltaTime;
        }
        if (mouse == null) return;

        float wheel = mouse.scroll.ReadValue().y;
        if (!Mathf.Approximately(wheel, 0f)) firstColumnTarget = Mathf.Round(firstColumnTarget) - Mathf.Sign(wheel) * wheelStep;

        var cam = Camera.main;
        if (cam == null) return;
        float worldX = cam.ScreenToWorldPoint(mouse.position.ReadValue()).x;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragging = true;
            dragStartX = worldX;
            dragStartColumn = firstColumnTarget;
        }
        if (!mouse.leftButton.isPressed) dragging = false;
        if (dragging) firstColumnTarget = dragStartColumn - (worldX - dragStartX) / (spacing * Mathf.Max(CameraRig.Zoom, 0.01f));
    }

    private Vector2 MouseTarget()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null || dragging) return mouseShift;
        Vector2 screen = mouse.position.ReadValue();
        return new Vector2(
            Mathf.Clamp(screen.x / Mathf.Max(cam.pixelWidth, 1) * 2f - 1f, -1f, 1f),
            Mathf.Clamp(screen.y / Mathf.Max(cam.pixelHeight, 1) * 2f - 1f, -1f, 1f));
    }

    // 1 — внутри окна, 0 — за краем, у самого края плавно гаснет.
    private float Fade(Vector3 local, float width)
    {
        var position = content.localPosition + local;
        float halfWidth = windowWidth / 2f - margin + spacing * 0.25f;
        float halfHeight = windowHeight / 2f;
        float soft = Mathf.Max(width, 0.01f);
        float x = (halfWidth - Mathf.Abs(position.x)) / soft;
        float y = (halfHeight - Mathf.Abs(position.y)) / soft;
        return Mathf.Clamp01(Mathf.Min(x, y));
    }

    private void PlaceMarker(float visibility)
    {
        if (currentMarker == null) return;
        bool show = current != null && Fade(current.transform.localPosition, fadeWidth) > 0.5f;
        currentMarker.gameObject.SetActive(show);
        if (!show) return;
        if (markerSprite != null)
        {
            var color = markerColor;
            color.a *= visibility;
            markerSprite.color = color;
        }
        float bob = Mathf.Round(Mathf.Sin(time * markerBobSpeed * Mathf.PI * 2f) * markerBob);
        var arrow = currentMarker.GetComponent<SpriteRenderer>();
        float half = arrow != null && arrow.sprite != null ? arrow.sprite.bounds.size.y / 2f : 0f;
        var top = transform.InverseTransformPoint(current.Top);
        currentMarker.localPosition = new Vector3(top.x, top.y + markerGap + half + bob, 0f);
    }
}
