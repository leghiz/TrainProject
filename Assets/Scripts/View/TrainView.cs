using System.Collections.Generic;
using UnityEngine;

// ExecuteAlways — только ради предпросмотра поезда в редакторе (см. ниже).
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

    public LocomotiveView Locomotive => locomotive;

    private float LocomotiveLeft => locomotive != null ? locomotive.transform.localPosition.x - locomotive.Width / 2f : 0f;
    private float GroundY => locomotive != null ? locomotive.transform.localPosition.y - locomotive.Height / 2f : 0f;

    public WagonView AddWagon()
    {
        var wagon = Instantiate(wagonPrefab, transform);
        wagons.Add(wagon);
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

    private void ApplyFog()
    {
        var center = wagons.Count > 0 ? wagons[wagons.Count / 2] : null;
        if (locomotive != null) locomotive.SetFogged(fog && center != null);
        foreach (var wagon in wagons) wagon.SetFogged(fog && wagon != center);
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

    // Обратное к CellsToX: сколько клеток за локомотивом до точки x (в координатах Train).
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
    // Предпросмотр: вагоны-картинки только в редакторе. Не сохраняются в сцену,
    // не выделяются кликом и исчезают при запуске игры.
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

    // В редакторе Update срабатывает, когда что-то меняется в сцене: подвинули локомотив — вагоны едут следом.
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
