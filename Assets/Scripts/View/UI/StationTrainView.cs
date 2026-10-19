using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Второй шаг выбора вагона: наш состав в ряд (локомотив справа), взятый вагон чуть приподнят и стоит в пустом месте.
// Место — ближайший к мышке стык: вагоны раздвигаются, вагон переезжает туда; клик — опускается. ПКМ или Esc — назад к выбору.
public class StationTrainView : MonoBehaviour
{
    [Tooltip("Ряд состава: в нём раскладываются вагоны (уменьшается, если поезд не влезает)")]
    [SerializeField] private Transform cars;
    [Tooltip("Образец вагона в ряду (выключен, код делает из него копии)")]
    [SerializeField] private SpriteRenderer carTemplate;
    [Tooltip("Образец того, что стоит в вагоне: станок или ящик")]
    [SerializeField] private SpriteRenderer partTemplate;
    [Tooltip("Чем рисовать ящик без картинки")]
    [SerializeField] private Sprite blankSprite;

    [Header("Раскладка, пиксели от центра экрана")]
    [Tooltip("Высота, на которой стоят колёса состава")]
    [SerializeField] private float rowBottom = -40f;
    [Tooltip("Высота колёс взятого вагона, пока его двигают вдоль состава: чуть выше остальных")]
    [SerializeField] private float carryBottom = -34f;
    [Tooltip("Сцепка между вагонами")]
    [SerializeField] private float gap = 6f;
    [Tooltip("Отступ ряда от краёв экрана")]
    [SerializeField] private float sideMargin = 12f;

    [Header("Движение")]
    [Tooltip("Как быстро вагоны раздвигаются: чем больше, тем быстрее")]
    [SerializeField] private float slideSpeed = 14f;
    [Tooltip("Как быстро взятый вагон переезжает к новому месту")]
    [SerializeField] private float carrySpeed = 16f;
    [Tooltip("Сколько секунд вагон прилетает из ряда выбора в руку")]
    [SerializeField] private float pickTime = 0.25f;

    [Header("Вагон")]
    [Tooltip("Размер клетки вагона, пиксели")]
    [SerializeField] private float cellSize = 16f;
    [Tooltip("Высота рамы с колёсами внизу спрайта, пиксели: станки стоят над ней")]
    [SerializeField] private float chassisHeight = 7f;

    public event Action<int> Chosen;
    public event Action Cancelled;

    private class Car
    {
        public Transform Root;
        public SpriteRenderer Body;
        public float Width;
        public float X;
        public bool Placed;
    }

    private readonly List<Car> wagons = new List<Car>();
    private Car locomotive;
    private Car carried;
    private Vector3 carriedFrom;
    private float carriedTime;
    private bool active;
    private bool locked;
    private int index;
    private int startFrame;

    public void Clear()
    {
        foreach (var car in wagons) Destroy(car.Root.gameObject);
        wagons.Clear();
        if (locomotive != null) Destroy(locomotive.Root.gameObject);
        if (carried != null) Destroy(carried.Root.gameObject);
        locomotive = null;
        carried = null;
        active = false;
        locked = false;
    }

    public void SetLocomotive(Sprite sprite) => locomotive = Make(sprite, null, 0);

    public Transform AddWagon(Sprite sprite, Material material)
    {
        var car = Make(sprite, material, 0);
        wagons.Add(car);
        return car.Root;
    }

    // Станок или ящик в вагоне: клетка от левого нижнего угла и сколько клеток занимает.
    public void AddPart(Transform wagon, Sprite sprite, Material material, Vector2Int cell, Vector2Int size)
    {
        if (wagon == null || partTemplate == null) return;
        var body = wagon.GetComponent<SpriteRenderer>();
        if (body == null || body.sprite == null) return;

        var part = Instantiate(partTemplate, wagon);
        part.gameObject.SetActive(true);
        part.sortingOrder = body.sortingOrder + 1;
        if (material != null) part.sharedMaterial = material;
        var area = (Vector2)size * cellSize;
        var origin = (Vector2)body.sprite.bounds.min + new Vector2(0f, chassisHeight);
        part.transform.localPosition = origin + (Vector2)cell * cellSize + area / 2f;
        if (sprite != null)
        {
            part.sprite = sprite;
            part.transform.localScale = Vector3.one;
        }
        else if (blankSprite != null)
        {
            part.sprite = blankSprite;
            var blankSize = (Vector2)blankSprite.bounds.size;
            part.transform.localScale = new Vector3(area.x / blankSize.x, area.y / blankSize.y, 1f);
        }
    }

    // Взятый вагон: вылетает из того места, где стоял в ряду выбора, и дальше ездит за мышкой.
    public Transform Carry(Sprite sprite, Material material, Vector3 fromWorld)
    {
        carried = Make(sprite, material, 4);
        carriedFrom = fromWorld;
        carriedTime = 0f;
        carried.Root.position = fromWorld;
        active = true;
        locked = false;
        index = wagons.Count;
        startFrame = Time.frameCount;
        Layout(true);
        return carried.Root;
    }

    private Car Make(Sprite sprite, Material material, int orderOffset)
    {
        var body = Instantiate(carTemplate, cars);
        body.gameObject.SetActive(true);
        body.sprite = sprite;
        body.sortingOrder = carTemplate.sortingOrder + orderOffset;
        if (material != null) body.sharedMaterial = material;
        return new Car { Root = body.transform, Body = body, Width = sprite != null ? sprite.bounds.size.x : 0f };
    }

    private void Update()
    {
        if (!active || locked || Time.frameCount <= startFrame) return;

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
        {
            Cancelled?.Invoke();
            return;
        }

        index = IndexUnderMouse();
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !PointerOverUI.Check(this))
        {
            locked = true;
            Chosen?.Invoke(index);
        }
    }

    private void LateUpdate()
    {
        if (!active) return;
        Layout(false);
    }

    private float Total(bool withGap)
    {
        float total = locomotive != null ? locomotive.Width : 0f;
        foreach (var car in wagons) total += gap + car.Width;
        if (withGap && carried != null) total += gap + carried.Width;
        return total;
    }

    private float Scale()
    {
        float available = VisibleWidth() - sideMargin * 2f;
        float total = Total(true);
        return total > available && total > 0f ? available / total : 1f;
    }

    // Раскладка справа налево: локомотив, вагоны, а в месте index — пустое место под взятый вагон.
    private void Layout(bool snap)
    {
        float scale = Scale();
        cars.localScale = new Vector3(scale, scale, 1f);
        float step = snap ? 1f : 1f - Mathf.Exp(-slideSpeed * Time.unscaledDeltaTime);

        float right = Total(true) / 2f;
        float slot = right;
        if (locomotive != null)
        {
            Move(locomotive, right - locomotive.Width / 2f, rowBottom / scale, step);
            right -= locomotive.Width + gap;
        }
        for (int i = 0; i <= wagons.Count; i++)
        {
            if (i == index && carried != null)
            {
                slot = right - carried.Width / 2f;
                right -= carried.Width + gap;
            }
            if (i == wagons.Count) break;
            var car = wagons[i];
            Move(car, right - car.Width / 2f, rowBottom / scale, step);
            right -= car.Width + gap;
        }

        if (carried == null) return;
        carriedTime += Time.unscaledDeltaTime;
        float targetX = slot;
        float bottom = locked ? rowBottom : carryBottom;
        var target = new Vector3(targetX, bottom / scale - carried.Body.sprite.bounds.min.y, 0f);
        if (carriedTime < pickTime)
        {
            float t = pickTime > 0f ? carriedTime / pickTime : 1f;
            t = 1f - (1f - t) * (1f - t);
            var from = cars.InverseTransformPoint(carriedFrom);
            carried.Root.localPosition = Vector3.Lerp(new Vector3(from.x, from.y, 0f), target, t);
            return;
        }
        float carry = 1f - Mathf.Exp(-carrySpeed * Time.unscaledDeltaTime);
        var position = Vector3.Lerp(carried.Root.localPosition, target, carry);
        carried.Root.localPosition = new Vector3(position.x, position.y, 0f);
    }

    private void Move(Car car, float x, float bottom, float step)
    {
        if (!car.Placed)
        {
            car.X = x;
            car.Placed = true;
        }
        car.X = Mathf.Lerp(car.X, x, step);
        float y = bottom - (car.Body.sprite != null ? car.Body.sprite.bounds.min.y : 0f);
        car.Root.localPosition = new Vector3(Mathf.Round(car.X), Mathf.Round(y), 0f);
    }

    // Куда встанет вагон: ближайший к мышке стык состава (считается по ряду без пустого места, чтобы не дёргалось).
    private int IndexUnderMouse()
    {
        float mouseX = MouseLocalX();
        float left = Total(false) / 2f - (locomotive != null ? locomotive.Width : 0f);
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i <= wagons.Count; i++)
        {
            float distance = Mathf.Abs(mouseX - (left - gap / 2f));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
            if (i < wagons.Count) left -= gap + wagons[i].Width;
        }
        return best;
    }

    private float MouseLocalX()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return 0f;
        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        return cars.InverseTransformPoint(world).x;
    }

    private float VisibleWidth()
    {
        var cam = Camera.main;
        if (cam == null) return 320f;
        float zoom = Mathf.Max(CameraRig.Zoom, 0.01f);
        return cam.orthographicSize / zoom * 2f * cam.aspect;
    }
}
