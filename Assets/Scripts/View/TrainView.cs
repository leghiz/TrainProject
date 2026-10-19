using System.Collections.Generic;
using UnityEngine;

public class TrainView : MonoBehaviour
{
    [Tooltip("Локомотив из сцены (дочерний объект Train). Где его поставили — там он и стоит, вагоны цепляются за ним влево")]
    [SerializeField] private LocomotiveView locomotive;
    [SerializeField] private WagonView wagonPrefab;

    [Tooltip("Расстояние между вагонами (сцепка), в единицах мира")]
    [SerializeField] private float gap = 6f;

    [Tooltip("Размер одной клетки в единицах мира (при PPU = 1 это 16)")]
    [SerializeField] private float cellSize = 16f;

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
        Destroy(wagon.gameObject);
    }
    
    public void Arrange()
    {
        float right = locomotive != null ? LocomotiveLeft - gap : 0f;
        foreach (var wagon in wagons)
        {
            Place(wagon.transform, wagon.Width, wagon.Height, right);
            right -= wagon.Width + gap;
        }
        ApplyFog(); 
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

    private void Place(Transform target, float width, float height, float right) =>
        target.localPosition = new Vector3(right - width / 2f, GroundY + height / 2f, 0f);
}
