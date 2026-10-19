using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum LocomotiveSlot { Boiler, Workbench, Welder }

public enum LocomotivePart { None, Body, Boiler, Workbench, Welder, Ready }

public class LocomotiveView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;

    [Tooltip("Точка у левого края полоски хп. Её масштаб по X = доля оставшегося хп")]
    [SerializeField] private Transform hpFill;

    [Tooltip("Число хп рядом с полоской, например 150/200 (можно не заполнять)")]
    [SerializeField] private TextMesh hpText;

    [Tooltip("Где появляется тултип при наведении на Коптильню, Верстак, Сварочный")]
    [SerializeField] private Transform tooltipPoint;

    [Header("Места станков (центры мест — двигать под спрайт локомотива)")]
    [SerializeField] private MachineView machinePrefab;
    [SerializeField] private Transform boilerSlot;
    [SerializeField] private Transform workbenchSlot;
    [SerializeField] private Transform welderSlot;

    [Tooltip("Размер одной клетки в единицах мира (при PPU = 1 это 16)")]
    [SerializeField] private float cellSize = 16f;

    [Tooltip("Сколько клеток занимает верстак: по этой области ловится клик")]
    [SerializeField] private Vector2Int workbenchCells = new Vector2Int(2, 1);

    [Tooltip("Сколько клеток занимает Коптильня: по этой области ловится клик (загрузить дерево)")]
    [SerializeField] private Vector2Int boilerCells = new Vector2Int(2, 1);

    [Header("Обшивка")]
    [Tooltip("Точки, куда вешаются листы. Двигать под спрайт локомотива. Сколько точек — столько и листов (должно совпадать с maxPlates у Locomotive.asset)")]
    [SerializeField] private Transform[] plateSlots;

    [Tooltip("Размер области вокруг точки, над которой можно отпустить лист (в единицах мира)")]
    [SerializeField] private Vector2 plateSlotSize = new Vector2(16f, 16f);

    [Header("Готовый станок с верстака")]
    [Tooltip("Иконка над локомотивом: её хватают мышкой и тащат на вагон")]
    [SerializeField] private SpriteRenderer readyIcon;

    [Header("Улучшение Коптильни")]
    [Tooltip("Кнопка улучшения на локомотиве (с ценой)")]
    [SerializeField] private BoilerUpgradeView boilerUpgrade;

    [Header("Прогресс крафта")]
    [Tooltip("Полоска целиком: видна, пока верстак что-то делает")]
    [SerializeField] private GameObject craftProgressBar;
    [Tooltip("Точка у левого края полоски: её масштаб по X = доля готовности")]
    [SerializeField] private Transform craftProgressFill;

    [Header("Разрушение")]
    [Tooltip("Что происходит, когда локомотив разрушен: анимация, другой спрайт, дым — настраивается здесь без кода")]
    [SerializeField] private UnityEvent onDestroyed;

    [Header("Подсветка установки")]
    [SerializeField] private SpriteRenderer placementPreview;
    [SerializeField] private Color canPlaceColor = new Color(0.3f, 1f, 0.3f, 0.45f);
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0.3f, 0.3f, 0.45f);

    public event Action WelderSlotHovered;
    public event Action WelderSlotExited;
    public event Action WelderSlotReleased;
    public event Action WorkbenchClicked;
    public event Action BoilerClicked;
    public event Action ReadyIconPressed;
    public event Action<int> PlateSlotHovered;
    public event Action PlateSlotExited;
    public event Action<int> PlateSlotReleased;
    public event Action<LocomotivePart> PartHovered;
    public event Action<LocomotivePart> PartRightPressed;

    public bool IsWelderSlotHovered { get; private set; }
    public int? HoveredPlateSlot { get; private set; }
    public LocomotivePart HoveredPart { get; private set; }
    public int PlateSlotCount => plateSlots != null ? plateSlots.Length : 0;

    public void SetSprite(Sprite sprite) => body.sprite = sprite;

    public float Width => body.sprite.bounds.size.x * body.transform.localScale.x;
    public float Height => body.sprite.bounds.size.y * body.transform.localScale.y;

    private bool fogged;

    public void SetFogged(bool value)
    {
        fogged = value;
        foreach (var outline in GetComponentsInChildren<HoverOutline>(true)) outline.enabled = !value;
        if (boilerUpgrade != null) boilerUpgrade.SetBlocked(value);
    }

    public void SetHp(int hp, int maxHp)
    {
        hpFill.localScale = new Vector3(maxHp > 0 ? (float)hp / maxHp : 0f, 1f, 1f);
        if (hpText != null) hpText.text = $"{hp}/{maxHp}";
    }

    public Vector3 TooltipPoint => tooltipPoint != null ? tooltipPoint.position : transform.position + Vector3.up * Height / 2f;

    public Vector3 ReadyIconCenter => readyIcon.bounds.center;

    public void ShowDestroyed() => onDestroyed?.Invoke();

    public BoilerUpgradeView BoilerUpgrade => boilerUpgrade;

    public void SetCraftProgress(float? progress)
    {
        if (craftProgressBar != null) craftProgressBar.SetActive(progress.HasValue);
        if (progress.HasValue && craftProgressFill != null)
            craftProgressFill.localScale = new Vector3(Mathf.Clamp01(progress.Value), 1f, 1f);
    }
    
    public MachineView AddMachine(LocomotiveSlot slot)
    {
        var machine = Instantiate(machinePrefab, SlotAnchor(slot).position, Quaternion.identity, transform);
        if (fogged) foreach (var outline in machine.GetComponentsInChildren<HoverOutline>(true)) outline.enabled = false;
        return machine;
    }

    public void RemoveMachine(MachineView machine) => Destroy(machine.gameObject);

    public void ShowWelderPreview(bool canPlace)
    {
        placementPreview.gameObject.SetActive(true);
        placementPreview.color = canPlace ? canPlaceColor : cannotPlaceColor;
        placementPreview.transform.position = welderSlot.position;
        placementPreview.transform.localScale = new Vector3(cellSize, cellSize, 1f);
    }

    public void HidePlacementPreview() => placementPreview.gameObject.SetActive(false);

    public MachineView AddPlate(int slot)
    {
        var plate = Instantiate(machinePrefab, plateSlots[slot].position, Quaternion.identity, transform);
        if (fogged) foreach (var outline in plate.GetComponentsInChildren<HoverOutline>(true)) outline.enabled = false;
        return plate;
    }

    public void ShowPlatePreview(int slot, bool canPlace)
    {
        placementPreview.gameObject.SetActive(true);
        placementPreview.color = canPlace ? canPlaceColor : cannotPlaceColor;
        placementPreview.transform.position = plateSlots[slot].position;
        placementPreview.transform.localScale = new Vector3(plateSlotSize.x, plateSlotSize.y, 1f);
    }

    public void ShowReady(Sprite sprite)
    {
        readyIcon.sprite = sprite;
        readyIcon.gameObject.SetActive(true);
    }

    public void HideReady() => readyIcon.gameObject.SetActive(false);

    private void Update()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        bool hovered = !fogged && IsOver(world, welderSlot, Vector2Int.one);
        int? plateSlot = fogged ? null : PlateSlotAt(world);

        bool welderChanged = hovered != IsWelderSlotHovered;
        bool plateChanged = plateSlot != HoveredPlateSlot;
        IsWelderSlotHovered = hovered;
        HoveredPlateSlot = plateSlot;
        if (welderChanged && !hovered) WelderSlotExited?.Invoke();
        if (plateChanged && !plateSlot.HasValue) PlateSlotExited?.Invoke();
        if (welderChanged && hovered) WelderSlotHovered?.Invoke();
        if (plateChanged && plateSlot.HasValue) PlateSlotHovered?.Invoke(plateSlot.Value);

        var part = fogged ? LocomotivePart.None : PartAt(world, hovered);
        if (part != HoveredPart)
        {
            HoveredPart = part;
            PartHovered?.Invoke(part);
        }

        if (fogged || PointerOverUI.Check()) return;

        if (mouse.rightButton.wasPressedThisFrame && part != LocomotivePart.None) PartRightPressed?.Invoke(part);

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (hovered) WelderSlotReleased?.Invoke();
            if (plateSlot.HasValue) PlateSlotReleased?.Invoke(plateSlot.Value);
        }

        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (IsOverReadyIcon(world)) ReadyIconPressed?.Invoke();
        else if (IsOver(world, workbenchSlot, workbenchCells)) WorkbenchClicked?.Invoke();
        else if (IsOver(world, boilerSlot, boilerCells)) BoilerClicked?.Invoke();
    }

    private bool IsOver(Vector2 world, Transform slot, Vector2Int cells)
    {
        Vector2 offset = world - (Vector2)slot.position;
        return Mathf.Abs(offset.x) <= cells.x * cellSize / 2f && Mathf.Abs(offset.y) <= cells.y * cellSize / 2f;
    }

    private LocomotivePart PartAt(Vector2 world, bool overWelder)
    {
        if (!gameObject.activeInHierarchy) return LocomotivePart.None;
        if (IsOverReadyIcon(world)) return LocomotivePart.Ready;
        if (overWelder) return LocomotivePart.Welder;
        if (IsOver(world, workbenchSlot, workbenchCells)) return LocomotivePart.Workbench;
        if (IsOver(world, boilerSlot, boilerCells)) return LocomotivePart.Boiler;
        var bounds = body.bounds;
        bool onBody = world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
        return onBody ? LocomotivePart.Body : LocomotivePart.None;
    }

    private int? PlateSlotAt(Vector2 world)
    {
        if (plateSlots == null) return null;
        for (int i = 0; i < plateSlots.Length; i++)
        {
            if (plateSlots[i] == null) continue;
            Vector2 offset = world - (Vector2)plateSlots[i].position;
            if (Mathf.Abs(offset.x) <= plateSlotSize.x / 2f && Mathf.Abs(offset.y) <= plateSlotSize.y / 2f) return i;
        }
        return null;
    }

    private bool IsOverReadyIcon(Vector2 world)
    {
        if (!readyIcon.gameObject.activeSelf || readyIcon.sprite == null) return false;
        var bounds = readyIcon.bounds;
        return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }

    private Transform SlotAnchor(LocomotiveSlot slot)
    {
        switch (slot)
        {
            case LocomotiveSlot.Boiler: return boilerSlot;
            case LocomotiveSlot.Workbench: return workbenchSlot;
            default: return welderSlot;
        }
    }
}
