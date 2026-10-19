using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


public class WagonView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body; 

    [Tooltip("Точка у левого края полоски хп. Её масштаб по X = доля оставшегося хп")]
    [SerializeField] private Transform hpFill;

    [Tooltip("Полоска хп целиком: видна, только пока мышь над вагоном")]
    [SerializeField] private GameObject hpBar;

    [Tooltip("Где появляется тултип: сдвиг от середины верхнего края вагона (вагоны разной высоты)")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 4f);

    [Header("Станки")]
    [SerializeField] private MachineView machinePrefab;
    [SerializeField] private CargoBoxView boxPrefab;

    [Tooltip("Размер одной клетки сетки в пикселях спрайта")]
    [SerializeField] private int cellSize = 16;

    [Tooltip("Высота рамы с колёсами внизу спрайта в пикселях: сетка начинается выше неё")]
    [SerializeField] private int chassisHeight = 7;

    [Header("Отцепился — откатывается влево")]
    [Tooltip("Скорость в первый момент, в пикселях игры в секунду")]
    [SerializeField] private float rollStartSpeed = 10f;
    [Tooltip("Насколько быстро отстаёт: поезд уходит вперёд, вагон всё быстрее уезжает влево")]
    [SerializeField] private float rollAcceleration = 40f;
    [Tooltip("Что ещё происходит, когда вагон отцепился: звук, частицы, обломки — без кода")]
    [SerializeField] private UnityEvent onDetached;

    [Header("Подсветка установки")]
    [SerializeField] private SpriteRenderer placementPreview;
    [SerializeField] private Color canPlaceColor = new Color(0.3f, 1f, 0.3f, 0.45f);
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0.3f, 0.3f, 0.45f);

    public event Action<Vector2Int> CellHovered; 
    public event Action HoverExited; 
    public event Action<Vector2Int> CellReleased;

    public Vector2Int? HoveredCell { get; private set; } 

    private Vector2Int gridSize;

    public void SetSprite(Sprite sprite) => body.sprite = sprite;

    public Vector3 TooltipPoint => transform.position + Vector3.up * Height / 2f + (Vector3)tooltipOffset;

    private void Awake()
    {
        if (hpBar != null) hpBar.SetActive(false);
    }
    
    public float Width => body.sprite.bounds.size.x * body.transform.localScale.x;
    public float Height => body.sprite.bounds.size.y * body.transform.localScale.y;
    
    private bool fogged;

    public void SetFogged(bool value)
    {
        fogged = value;
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = !value;
    }

    public void SetGridSize(Vector2Int size) => gridSize = size;

    public void SetHp(float normalized) => hpFill.localScale = new Vector3(normalized, 1f, 1f);

    private bool rolling;
    private float rollSpeed;

    public void RollAway()
    {
        rolling = true;
        rollSpeed = rollStartSpeed;
        HoveredCell = null;
        if (hpBar != null) hpBar.SetActive(false);
        HidePlacementPreview();
        onDetached?.Invoke();
    }

    public MachineView AddMachine(Vector2Int cell, Vector2Int size)
    {
        var machine = Instantiate(machinePrefab, transform);
        machine.transform.localPosition = AreaCenter(cell, size);
        if (fogged) foreach (var r in machine.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
        return machine;
    }

    public void ShowPlacementPreview(Vector2Int cell, Vector2Int size, bool canPlace)
    {
        placementPreview.gameObject.SetActive(true);
        placementPreview.color = canPlace ? canPlaceColor : cannotPlaceColor;
        placementPreview.transform.localPosition = AreaCenter(cell, size);
        placementPreview.transform.localScale = new Vector3(size.x * CellUnits, size.y * CellUnits, 1f);
    }

    public void HidePlacementPreview() => placementPreview.gameObject.SetActive(false);

    public void RemoveMachine(MachineView machine) => Destroy(machine.gameObject);
    
    public CargoBoxView AddBox(Vector2Int cell, Vector2Int size, Sprite sprite)
    {
        var box = Instantiate(boxPrefab, transform);
        box.transform.localPosition = AreaCenter(cell, size);
        box.Setup(sprite, (Vector2)size * CellUnits);
        if (fogged) foreach (var r in box.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
        return box;
    }

    public void RemoveBox(CargoBoxView box) => Destroy(box.gameObject);

    private void Update()
    {
        if (rolling)
        {
            Roll();
            return;
        }

        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 local = transform.InverseTransformPoint(world);
        Vector2Int? cell = fogged ? null : LocalToCell(local);

        if (cell != HoveredCell)
        {
            HoveredCell = cell;
            if (hpBar != null) hpBar.SetActive(cell.HasValue);
            if (cell.HasValue) CellHovered?.Invoke(cell.Value);
            else HoverExited?.Invoke();
        }

        if (cell.HasValue && mouse.leftButton.wasReleasedThisFrame && !PointerOverUI.Check()) CellReleased?.Invoke(cell.Value);
    }
    
    private void Roll()
    {
        rollSpeed += rollAcceleration * Time.deltaTime;
        transform.position += Vector3.left * rollSpeed * Time.deltaTime;

        var cam = Camera.main;
        if (cam == null) return;
        float screenLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        if (transform.position.x + Width / 2f < screenLeft) Destroy(gameObject);
    }

    public Vector3 AreaTop(Vector2Int cell, Vector2Int size) =>
        transform.TransformPoint(AreaCenter(cell, size) + Vector2.up * size.y * CellUnits / 2f);

    public Vector2 CellToLocal(Vector2Int cell) => GridOrigin + (Vector2)cell * CellUnits;
    
    private Vector2Int? LocalToCell(Vector2 local)
    {
        Vector2 p = (local - GridOrigin) / CellUnits;
        var cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        bool inside = cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;
        return inside ? cell : (Vector2Int?)null;
    }

    private Vector2 AreaCenter(Vector2Int cell, Vector2Int size) => CellToLocal(cell) + (Vector2)size * CellUnits / 2f;
    private float CellUnits => cellSize / body.sprite.pixelsPerUnit;

    private Vector2 GridOrigin =>
        (Vector2)body.transform.localPosition
        + (Vector2)body.sprite.bounds.min
        + new Vector2(0f, chassisHeight / body.sprite.pixelsPerUnit);
}
