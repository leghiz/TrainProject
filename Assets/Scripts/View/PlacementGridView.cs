using System;
using System.Collections.Generic;
using UnityEngine;

public class PlacementGridView : MonoBehaviour
{
    [SerializeField] private Sprite cellSprite;
    [SerializeField] private Material material;
    [SerializeField] private int sortingOrder = 3;
    [Tooltip("Спрайт вагона: в тумане он спрятан — прячем и сетку")]
    [SerializeField] private SpriteRenderer hideWith;

    [Header("Цвета")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color blockedColor = new Color(1f, 0.35f, 0.3f, 0.85f);

    [Header("Анимация")]
    [Tooltip("Сколько секунд клетка проявляется и гаснет")]
    [SerializeField] private float fadeTime = 0.15f;
    [Tooltip("Задержка между соседними клетками при появлении: волна слева направо")]
    [SerializeField] private float stagger = 0.025f;
    [Tooltip("Сколько секунд клетка загорается под мышкой")]
    [SerializeField] private float highlightTime = 0.08f;
    [Tooltip("Насколько клетка раздувается, когда на неё наводишь")]
    [SerializeField] private float hoverPop = 1.12f;
    [Tooltip("С какого размера клетка вырастает при появлении")]
    [SerializeField] private float appearScale = 0.6f;

    private class Cell
    {
        public SpriteRenderer Renderer;
        public float Delay;
        public float Shown;
        public float Lit;
        public bool Wanted;
    }

    private readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
    private readonly HashSet<Vector2Int> highlighted = new HashSet<Vector2Int>();
    private bool blocked;
    private float time;

    public void Show(IReadOnlyList<Vector2Int> freeCells, Func<Vector2Int, Vector2> localCenter)
    {
        time = 0f;
        foreach (var cell in cells.Values) cell.Wanted = false;

        foreach (var key in freeCells)
        {
            if (!cells.TryGetValue(key, out var cell))
            {
                var go = new GameObject($"Cell {key.x},{key.y}");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = cellSprite;
                if (material != null) renderer.sharedMaterial = material;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                cell = new Cell { Renderer = renderer };
                cells[key] = cell;
            }
            cell.Renderer.transform.localPosition = localCenter(key);
            cell.Delay = (key.x + key.y * 0.5f) * stagger;
            cell.Wanted = true;
        }
    }

    public void Hide()
    {
        foreach (var cell in cells.Values) cell.Wanted = false;
        highlighted.Clear();
    }

    public void Highlight(Vector2Int origin, Vector2Int size, bool canPlace)
    {
        highlighted.Clear();
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                highlighted.Add(origin + new Vector2Int(x, y));
        blocked = !canPlace;
    }

    public void ClearHighlight() => highlighted.Clear();

    private void LateUpdate()
    {
        time += Time.unscaledDeltaTime;
        float fadeStep = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
        float litStep = highlightTime > 0f ? Time.unscaledDeltaTime / highlightTime : 1f;
        bool hidden = hideWith != null && !hideWith.enabled;

        foreach (var pair in cells)
        {
            var cell = pair.Value;
            bool on = cell.Wanted && time >= cell.Delay;
            cell.Shown = Mathf.MoveTowards(cell.Shown, on ? 1f : 0f, fadeStep);
            cell.Lit = Mathf.MoveTowards(cell.Lit, cell.Wanted && highlighted.Contains(pair.Key) ? 1f : 0f, litStep);

            var renderer = cell.Renderer;
            renderer.enabled = !hidden && cell.Shown > 0f;
            if (!renderer.enabled) continue;

            var color = Color.Lerp(idleColor, blocked ? blockedColor : hoverColor, cell.Lit);
            color.a *= cell.Shown;
            renderer.color = color;

            float back = cell.Shown - 1f;
            float appear = Mathf.LerpUnclamped(appearScale, 1f, 1f + 2.7f * back * back * back + 1.7f * back * back);
            float scale = appear * Mathf.Lerp(1f, hoverPop, cell.Lit);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
