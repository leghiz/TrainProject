using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class TrainView : MonoBehaviour
{
    [Tooltip("Локомотив из сцены (дочерний объект Train). Где его поставили — там он и стоит, вагоны цепляются за ним влево")]
    [SerializeField] private LocomotiveView locomotive;
    [SerializeField] private WagonView wagonPrefab;

    [Tooltip("Расстояние между вагонами (сцепка), в единицах мира")]
    [SerializeField] private float gap = 6f;

    [Tooltip("Размер одной клетки в единицах мира (при PPU = 1 это 16)")]
    [SerializeField] private float cellSize = 16f;

    [Header("Предпросмотр в редакторе (в игре не используется)")]
    [Tooltip("Показывать за локомотивом вагоны-картинки, чтобы видеть весь поезд без Play")]
    [SerializeField] private bool showPreview = true;
    [Tooltip("Какие вагоны показать в предпросмотре")]
    [SerializeField] private WagonConfig[] previewWagons;

    private readonly List<WagonView> wagons = new List<WagonView>();
    public IReadOnlyList<WagonView> Wagons => wagons;

    public LocomotiveView Locomotive => locomotive;

    private float LocomotiveLeft => locomotive != null ? locomotive.transform.localPosition.x - locomotive.Width / 2f : 0f;
    private float GroundY => locomotive != null ? locomotive.transform.localPosition.y - locomotive.Height / 2f : 0f;

    // index — место в составе (0 — сразу за локомотивом); меньше 0 — в хвост.
    public WagonView AddWagon(int index = -1)
    {
        var wagon = Instantiate(wagonPrefab, transform);
        if (index < 0 || index > wagons.Count) wagons.Add(wagon);
        else wagons.Insert(index, wagon);
        return wagon;
    }

    public void RemoveWagon(WagonView wagon)
    {
        wagons.Remove(wagon);
        wagon.RollAway();
    }
    
    public void Arrange()
    {
        Arrange(wagons);
        ApplyFog();
    }

    private void Arrange(List<WagonView> views)
    {
        float right = locomotive != null ? LocomotiveLeft - gap : 0f;
        foreach (var wagon in views)
        {
            Place(wagon.transform, wagon.Width, wagon.Height, right);
            right -= wagon.Width + gap;
        }
    }
    
    private bool fog;

    public void SetFog(bool value)
    {
        fog = value;
        ApplyFog();
    }

    private Component fogFocus;

    // Что сейчас видно сквозь туман (вагон или локомотив ближе всех к камере) — по нему окно тумана.
    public Bounds? FogFocusBounds
    {
        get
        {
            if (!fog || fogFocus == null) return null;
            if (fogFocus is WagonView wagon) return new Bounds(wagon.transform.position, new Vector3(wagon.Width, wagon.Height, 0f));
            if (fogFocus is LocomotiveView engine) return new Bounds(engine.transform.position, new Vector3(engine.Width, engine.Height, 0f));
            return null;
        }
    }

    // Левый край последнего вагона (или локомотива, если вагонов нет), в мировых координатах.
    public float TailLeft
    {
        get
        {
            if (wagons.Count == 0) return locomotive != null ? locomotive.transform.position.x - locomotive.Width / 2f : transform.position.x;
            var last = wagons[wagons.Count - 1];
            return last.transform.position.x - last.Width / 2f;
        }
    }

    // Со станком в руке — вагон или локомотив под мышкой (с запасом на сцепку и высоту), иначе пусто.
    public Bounds? HeldOverBounds
    {
        get
        {
            var part = PartUnderHeldItem();
            if (part is WagonView wagon) return new Bounds(wagon.transform.position, new Vector3(wagon.Width, wagon.Height, 0f));
            if (part is LocomotiveView engine) return new Bounds(engine.transform.position, new Vector3(engine.Width, engine.Height, 0f));
            return null;
        }
    }

    private Component PartUnderHeldItem()
    {
        if (!Application.isPlaying || !HandView.Holding) return null;
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return null;

        Vector2 point = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        if (locomotive != null && IsNear(locomotive.transform.position, locomotive.Width, locomotive.Height, point)) return locomotive;
        foreach (var wagon in wagons)
            if (IsNear(wagon.transform.position, wagon.Width, wagon.Height, point)) return wagon;
        return null;
    }

    private bool IsNear(Vector3 center, float width, float height, Vector2 point) =>
        Mathf.Abs(point.x - center.x) <= width / 2f + gap / 2f
        && point.y >= center.y - height / 2f - cellSize / 2f
        && point.y <= center.y + height / 2f + cellSize * 1.5f;

    // В тумане видно то, к чему ближе камера; со станком в руке — то, над чем мышка.
    private Component FocusPart()
    {
        var held = PartUnderHeldItem();
        return held != null ? held : PartNearestToCamera();
    }

    private void ApplyFog()
    {
        fogFocus = fog ? FocusPart() : null;
        if (locomotive != null) locomotive.SetFogged(fog && fogFocus != locomotive);
        foreach (var wagon in wagons) wagon.SetFogged(fog && fogFocus != wagon);
    }

    private Component PartNearestToCamera()
    {
        var cam = Camera.main;
        float cameraX = cam != null ? cam.transform.position.x : 0f;
        Component nearest = locomotive;
        float best = locomotive != null ? Mathf.Abs(locomotive.transform.position.x - cameraX) : float.MaxValue;
        foreach (var wagon in wagons)
        {
            float distance = Mathf.Abs(wagon.transform.position.x - cameraX);
            if (distance < best) { best = distance; nearest = wagon; }
        }
        return nearest;
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && fog && FocusPart() != fogFocus) ApplyFog();
    }
    
    public float CellsToX(float cellsBehindLocomotive)
    {
        float x = LocomotiveLeft;
        if (cellsBehindLocomotive <= 0f) return x - cellsBehindLocomotive * cellSize;

        float remaining = cellsBehindLocomotive;
        foreach (var wagon in wagons)
        {
            x -= gap;
            float wagonCells = wagon.Width / cellSize;
            if (remaining <= wagonCells) return x - remaining * cellSize;
            remaining -= wagonCells;
            x -= wagon.Width;
        }
        return x - remaining * cellSize;
    }

    public float XToCells(float localX)
    {
        float x = LocomotiveLeft;
        if (localX >= x) return -(localX - x) / cellSize;

        float cells = 0f;
        foreach (var wagon in wagons)
        {
            x -= gap;
            if (localX >= x) return cells;
            if (localX >= x - wagon.Width) return cells + (x - localX) / cellSize;
            cells += wagon.Width / cellSize;
            x -= wagon.Width;
        }
        return cells + (x - localX) / cellSize;
    }

    private void Place(Transform target, float width, float height, float right) =>
        target.localPosition = new Vector3(right - width / 2f, GroundY + height / 2f, 0f);

#if UNITY_EDITOR
    private readonly List<WagonView> previews = new List<WagonView>();

    private void OnEnable()
    {
        if (!Application.isPlaying) RebuildPreview();
    }

    private void OnDisable() => ClearPreview();

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled && !Application.isPlaying) RebuildPreview();
        };
    }

    private void Update()
    {
        if (!Application.isPlaying && previews.Count > 0) Arrange(previews);
    }

    private void RebuildPreview()
    {
        ClearPreview();
        if (!showPreview || previewWagons == null || wagonPrefab == null) return;

        foreach (var config in previewWagons)
        {
            if (config == null) continue;
            var view = Instantiate(wagonPrefab, transform);
            view.name = "Preview " + config.displayName;
            view.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            UnityEditor.SceneVisibilityManager.instance.DisablePicking(view.gameObject, true);
            view.SetSprite(config.sprite);
            view.SetGridSize(config.size);
            previews.Add(view);
        }
        Arrange(previews);
    }

    private void ClearPreview()
    {
        foreach (var view in previews)
            if (view != null) DestroyImmediate(view.gameObject);
        previews.Clear();
    }
#endif
}
