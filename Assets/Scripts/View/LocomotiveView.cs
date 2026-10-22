using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum LocomotiveSlot { Boiler, Workbench, Welder }

public enum LocomotivePart { None, Body, Boiler, Workbench, Welder, Ready }

public class LocomotiveView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;

    [Tooltip("Полоска хп целиком: прячется, пока открыто меню коптильни или верстака")]
    [SerializeField] private GameObject hpBar;

    [Tooltip("Точка у левого края полоски хп. Её масштаб по X = доля оставшегося хп")]
    [SerializeField] private Transform hpFill;

    [Tooltip("Ширина полоски в пикселях: доля хп округляется до целого пикселя")]
    [SerializeField] private int hpWidth = 32;

    [Tooltip("Число хп рядом с полоской, например 150/200 (можно не заполнять)")]
    [SerializeField] private TextMesh hpText;

    [Tooltip("Где появляется тултип при наведении на Коптильню, Верстак, Сварочный")]
    [SerializeField] private Transform tooltipPoint;

    [Header("Места станков (центры мест — двигать под спрайт локомотива)")]
    [SerializeField] private MachineView machinePrefab;
    [Tooltip("Коптильня, которая уже стоит в префабе. Пусто — создаётся из Machine Prefab в BoilerSlot")]
    [SerializeField] private MachineView boiler;
    [Tooltip("Верстак, который уже стоит в префабе. Пусто — создаётся из Machine Prefab в WorkbenchSlot")]
    [SerializeField] private MachineView workbench;
    [Tooltip("Клетка под сварку: видна, пока сварка в руке")]
    [SerializeField] private SlotCellView welderCell;
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
    [Tooltip("Рамки под листы, по одной на точку: видны, пока лист в руке, под мышкой загораются")]
    [SerializeField] private SlotCellView[] plateCells;
    [Tooltip("Маленькие полоски хп под листами, по одной на точку: прячутся, пока листа нет")]
    [SerializeField] private GameObject[] plateHpBars;
    [Tooltip("Точки у левого края заливки этих полосок: их масштаб по X = хп листа")]
    [SerializeField] private Transform[] plateHpFills;
    [Tooltip("Ширина маленькой полоски хп, пиксели")]
    [SerializeField] private int plateHpWidth = 16;

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
    [Tooltip("Ширина полоски в пикселях: заполнение округляется до целого пикселя")]
    [SerializeField] private int craftProgressWidth = 16;

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

    // Коптильню и верстак двигают прямо в сцене: место для клика, меню и то, что висит над ними, едут следом.
    private void Awake()
    {
        Follow(boiler, boilerSlot, boilerUpgrade != null ? boilerUpgrade.transform : null);
        Follow(workbench, workbenchSlot, readyIcon != null ? readyIcon.transform : null,
               craftProgressBar != null ? craftProgressBar.transform : null);
    }

    private static void Follow(MachineView machine, Transform slot, params Transform[] riders)
    {
        if (machine == null || slot == null) return;
        var delta = machine.transform.position - slot.position;
        slot.position += delta;
        foreach (var rider in riders)
            if (rider != null) rider.position += delta;
    }

    // Камера ушла к другому вагону в тумане: интерфейс локомотива (хп, готовый станок, полоска крафта) прячется,
    // а открытые меню закрываются по этому событию.
    public event Action<bool> FoggedChanged;

    public void SetFogged(bool value)
    {
        bool changed = fogged != value;
        fogged = value;
        foreach (var outline in GetComponentsInChildren<HoverOutline>(true)) outline.enabled = !value;
        if (boilerUpgrade != null) boilerUpgrade.SetBlocked(value);
        ApplyHp();
        ApplyReady();
        ApplyCraftProgress();
        ApplyPlateHp();
        if (changed) FoggedChanged?.Invoke(value);
    }

    public void SetHp(int hp, int maxHp)
    {
        float share = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
        int width = Mathf.Max(hpWidth, 1);
        hpFill.localScale = new Vector3(Mathf.Round(share * width) / width, 1f, 1f);
        if (hpText != null) hpText.text = $"{hp}/{maxHp}";
    }

    private readonly System.Collections.Generic.HashSet<object> hpHiders = new System.Collections.Generic.HashSet<object>();

    // Меню коптильни и верстака ложатся на полоску хп — пока хоть одно открыто, полоска спрятана.
    public void HideHp(object who, bool hide)
    {
        if (hide) hpHiders.Add(who);
        else hpHiders.Remove(who);
        ApplyHp();
    }

    private void ApplyHp()
    {
        if (hpBar != null) hpBar.SetActive(hpHiders.Count == 0 && !fogged);
    }

    // Пока открыто меню, сама коптильня или верстак обведены.
    public void HighlightBoiler(bool value) => Pin(boiler, value);

    public void HighlightWorkbench(bool value) => Pin(workbench, value);

    private static void Pin(MachineView machine, bool value)
    {
        if (machine == null) return;
        var outline = machine.GetComponent<HoverOutline>();
        if (outline != null) outline.Pinned = value;
    }

    public Vector3 TooltipPoint => tooltipPoint != null ? tooltipPoint.position : transform.position + Vector3.up * Height / 2f;

    public Vector3 ReadyIconCenter => readyIcon.bounds.center;

    public void ShowDestroyed() => onDestroyed?.Invoke();

    public BoilerUpgradeView BoilerUpgrade => boilerUpgrade;

    // Обучение: на что указывать стрелкой.
    public Transform WorkbenchPoint => workbench != null ? workbench.transform : workbenchSlot;
    public Transform BoilerPoint => boiler != null ? boiler.transform : boilerSlot;
    public Transform ReadyIconPoint => readyIcon != null ? readyIcon.transform : null;

    private float? craftProgress;

    public void SetCraftProgress(float? progress)
    {
        craftProgress = progress;
        ApplyCraftProgress();
    }

    // Полоска крафта висит там же, где готовый станок, и прячется вместе с ним (меню коптильни).
    private void ApplyCraftProgress()
    {
        if (craftProgressBar != null) craftProgressBar.SetActive(craftProgress.HasValue && !readySuppressed && !fogged);
        if (!craftProgress.HasValue || craftProgressFill == null) return;
        int width = Mathf.Max(craftProgressWidth, 1);
        craftProgressFill.localScale = new Vector3(Mathf.Round(Mathf.Clamp01(craftProgress.Value) * width) / width, 1f, 1f);
    }
    
    public MachineView AddMachine(LocomotiveSlot slot)
    {
        var placed = slot == LocomotiveSlot.Boiler ? boiler : slot == LocomotiveSlot.Workbench ? workbench : null;
        if (placed != null)
        {
            placed.gameObject.SetActive(true);
            if (fogged) foreach (var outline in placed.GetComponentsInChildren<HoverOutline>(true)) outline.enabled = false;
            return placed;
        }
        var machine = Instantiate(machinePrefab, SlotAnchor(slot).position, Quaternion.identity, transform);
        if (fogged) foreach (var outline in machine.GetComponentsInChildren<HoverOutline>(true)) outline.enabled = false;
        return machine;
    }

    public void RemoveMachine(MachineView machine) => Destroy(machine.gameObject);

    public void ShowWelderCell(bool value)
    {
        if (welderCell != null) welderCell.Show(value);
    }

    public void ShowWelderPreview(bool canPlace)
    {
        if (welderCell != null)
        {
            welderCell.Highlight(canPlace);
            return;
        }
        placementPreview.gameObject.SetActive(true);
        placementPreview.color = canPlace ? canPlaceColor : cannotPlaceColor;
        placementPreview.transform.position = welderSlot.position;
        placementPreview.transform.localScale = new Vector3(cellSize, cellSize, 1f);
    }

    public void HidePlacementPreview()
    {
        placementPreview.gameObject.SetActive(false);
        if (welderCell != null) welderCell.ClearHighlight();
        if (plateCells != null) foreach (var cell in plateCells) if (cell != null) cell.ClearHighlight();
    }

    // Лист в руке — видны рамки всех мест под листы.
    public void ShowPlateCells(bool value)
    {
        if (plateCells == null) return;
        foreach (var cell in plateCells) if (cell != null) cell.Show(value);
    }

    private float?[] plateHp;

    // Хп листа маленькой полоской под ним; null — листа нет, полоска спрятана.
    public void SetPlateHp(int slot, float? share)
    {
        if (plateHp == null) plateHp = new float?[PlateSlotCount];
        if (slot < 0 || slot >= plateHp.Length) return;
        plateHp[slot] = share;
        ApplyPlateHp();
    }

    private void ApplyPlateHp()
    {
        if (plateHp == null || plateHpBars == null) return;
        int width = Mathf.Max(plateHpWidth, 1);
        for (int i = 0; i < plateHp.Length && i < plateHpBars.Length; i++)
        {
            if (plateHpBars[i] == null) continue;
            bool shown = plateHp[i].HasValue && !fogged;
            plateHpBars[i].SetActive(shown);
            if (!shown || plateHpFills == null || i >= plateHpFills.Length || plateHpFills[i] == null) continue;
            plateHpFills[i].localScale = new Vector3(Mathf.Round(Mathf.Clamp01(plateHp[i].Value) * width) / width, 1f, 1f);
        }
    }

    public MachineView AddPlate(int slot)
    {
        var plate = Instantiate(machinePrefab, plateSlots[slot].position, Quaternion.identity, transform);
        if (fogged) foreach (var outline in plate.GetComponentsInChildren<HoverOutline>(true)) outline.enabled = false;
        return plate;
    }

    public void ShowPlatePreview(int slot, bool canPlace)
    {
        if (plateCells != null && slot < plateCells.Length && plateCells[slot] != null)
        {
            for (int i = 0; i < plateCells.Length; i++)
                if (plateCells[i] != null && i != slot) plateCells[i].ClearHighlight();
            plateCells[slot].Highlight(canPlace);
            return;
        }
        placementPreview.gameObject.SetActive(true);
        placementPreview.color = canPlace ? canPlaceColor : cannotPlaceColor;
        placementPreview.transform.position = plateSlots[slot].position;
        placementPreview.transform.localScale = new Vector3(plateSlotSize.x, plateSlotSize.y, 1f);
    }

    private bool readyWanted;
    private bool readySuppressed;

    public void ShowReady(Sprite sprite)
    {
        readyIcon.sprite = sprite;
        readyWanted = true;
        ApplyReady();
    }

    public void HideReady()
    {
        readyWanted = false;
        ApplyReady();
    }

    // Пока открыто меню коптильни, готовый станок не показываем: он лёг бы на кнопку «+».
    public void SuppressReady(bool value)
    {
        readySuppressed = value;
        ApplyReady();
        ApplyCraftProgress();
    }

    private void ApplyReady() => readyIcon.gameObject.SetActive(readyWanted && !readySuppressed && !fogged);

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

        var part = fogged || PointerOverUI.Check() ? LocomotivePart.None : PartAt(world, hovered);
        if (part != HoveredPart)
        {
            HoveredPart = part;
            PartHovered?.Invoke(part);
        }

        if (fogged || PointerOverUI.Check()) return;

        if (mouse.rightButton.wasPressedThisFrame && part != LocomotivePart.None && !TutorialFocus.Active) PartRightPressed?.Invoke(part);

        if (mouse.leftButton.wasReleasedThisFrame && !TutorialFocus.Active)
        {
            if (hovered) WelderSlotReleased?.Invoke();
            if (plateSlot.HasValue) PlateSlotReleased?.Invoke(plateSlot.Value);
        }

        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (IsOverReadyIcon(world))
        {
            if (TutorialFocus.Allows(readyIcon.transform)) ReadyIconPressed?.Invoke();
        }
        else if (IsOver(world, workbenchSlot, workbenchCells))
        {
            if (!TutorialFocus.Allows(WorkbenchPoint)) return;
            AudioView.Click();
            WorkbenchClicked?.Invoke();
        }
        else if (IsOver(world, boilerSlot, boilerCells))
        {
            if (!TutorialFocus.Allows(BoilerPoint)) return;
            AudioView.Click();
            BoilerClicked?.Invoke();
        }
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
