using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MapNodeState { Passed, Current, Available, Locked }

// Станция на карте — иконка здания. Куда можно ехать: обводится при наведении, над ней всплывает погода
// по дороге туда, клик — ехать. Пройденные и пока недоступные станции притушены.
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [Tooltip("Белая обводка при наведении: работает только у станций, куда можно ехать")]
    [SerializeField] private HoverOutline outline;
    [Tooltip("Название станции под зданием")]
    [SerializeField] private TMP_Text label;
    [Tooltip("От нижнего края здания до названия, пиксели")]
    [SerializeField] private float labelGap = 1f;
    [Tooltip("Знак вопроса вместо здания: станция дальше, чем можно заглянуть")]
    [SerializeField] private TMP_Text unknownMark;

    [Header("Погода по дороге")]
    [Tooltip("Образец плашки погоды (выключен, код делает из него копии над зданием)")]
    [SerializeField] private MapEventView eventTemplate;
    [Tooltip("От верхнего края здания до кончика стрелки, пиксели")]
    [SerializeField] private float eventGap = 4f;
    [Tooltip("Расстояние между плашками, если погоды несколько, пиксели")]
    [SerializeField] private float eventSpacing = 18f;
    [Tooltip("Задержка между плашками при появлении, секунды")]
    [SerializeField] private float eventStagger = 0.05f;

    [Header("Цвет")]
    [SerializeField] private Color passedTint = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color lockedTint = new Color(0.72f, 0.72f, 0.72f, 1f);

    public event Action Clicked;

    // Верх здания в мире: над ним стрелка «мы здесь» и плашки погоды.
    public Vector3 Top => new Vector3(icon.bounds.center.x, icon.bounds.max.y, transform.position.z);

    // Станция в окне карты, а не за его краем.
    public bool Visible => fade > 0.5f;

    private readonly List<MapEventView> events = new List<MapEventView>();
    private MapNodeState state = MapNodeState.Locked;
    private Color tint = Color.white;
    private float fade = 1f;
    private bool hovered;
    private Color labelColor = Color.white;
    private Color unknownColor = Color.white;

    private void Awake()
    {
        if (eventTemplate != null) eventTemplate.gameObject.SetActive(false);
        if (label != null) labelColor = label.color;
        if (unknownMark != null) unknownColor = unknownMark.color;
    }

    public void SetStation(Sprite sprite, string name)
    {
        icon.sprite = sprite;
        if (label != null)
        {
            label.text = name;
            if (sprite != null)
            {
                float bottom = sprite.bounds.min.y * icon.transform.localScale.y + icon.transform.localPosition.y;
                float half = label.GetPreferredValues(name).y / 2f;
                label.transform.localPosition = new Vector3(0f, Mathf.Round(bottom - labelGap - half), 0f);
            }
        }
        PlaceEvents();
    }

    // Неизвестная станция: вместо здания и названия — знак вопроса.
    public void SetUnknown(bool value)
    {
        icon.enabled = !value;
        if (label != null) label.gameObject.SetActive(!value);
        if (unknownMark != null) unknownMark.gameObject.SetActive(value);
        ApplyColor();
    }

    public void SetState(MapNodeState value)
    {
        state = value;
        tint = value == MapNodeState.Passed ? passedTint : value == MapNodeState.Locked ? lockedTint : Color.white;
        if (outline != null) outline.enabled = value == MapNodeState.Available;
        ApplyColor();
    }

    // Погода по дороге сюда — по плашке на каждую.
    public void SetEvents(IReadOnlyList<Sprite> icons)
    {
        foreach (var item in events) Destroy(item.gameObject);
        events.Clear();
        if (eventTemplate == null || icons == null) return;
        foreach (var sprite in icons)
        {
            var item = Instantiate(eventTemplate, eventTemplate.transform.parent);
            item.gameObject.SetActive(true);
            item.Setup(sprite);
            events.Add(item);
        }
        PlaceEvents();
    }

    // Станция у края карты гаснет.
    public void SetFade(float value)
    {
        fade = value;
        ApplyColor();
        foreach (var item in events) item.SetFade(value);
    }

    private void ApplyColor()
    {
        var color = tint;
        color.a *= fade;
        icon.color = color;
        if (unknownMark != null)
        {
            var mark = unknownColor * tint;
            mark.a = unknownColor.a * fade;
            unknownMark.color = mark;
        }
        if (label == null) return;
        var text = labelColor * tint;
        text.a = labelColor.a * fade;
        label.color = text;
    }

    // Одна погода — над зданием; две — одна над, другая под названием; остальные — рядом с верхней.
    private void PlaceEvents()
    {
        if (icon.sprite == null) return;
        float top = icon.sprite.bounds.max.y * icon.transform.localScale.y + icon.transform.localPosition.y;
        float bottom = icon.sprite.bounds.min.y * icon.transform.localScale.y + icon.transform.localPosition.y;
        if (label != null && !string.IsNullOrEmpty(label.text))
            bottom = Mathf.Min(bottom, label.transform.localPosition.y - label.GetPreferredValues(label.text).y / 2f);

        int above = events.Count >= 2 ? events.Count - 1 : events.Count;
        float left = -(above - 1) * eventSpacing / 2f;
        int slot = 0;
        for (int i = 0; i < events.Count; i++)
        {
            bool below = events.Count >= 2 && i == 1;
            events[i].SetBelow(below);
            if (below)
            {
                events[i].transform.localPosition = new Vector3(0f, Mathf.Round(bottom - eventGap), 0f);
                continue;
            }
            events[i].transform.localPosition = new Vector3(Mathf.Round(left + slot * eventSpacing), Mathf.Round(top + eventGap), 0f);
            slot++;
        }
    }

    private void Update()
    {
        var mouse = Mouse.current;
        bool over = state == MapNodeState.Available && fade > 0.5f && mouse != null && IsOver(mouse) && !PointerOverUI.Check(this);
        if (over != hovered)
        {
            hovered = over;
            for (int i = 0; i < events.Count; i++) events[i].SetVisible(over, i * eventStagger);
        }
        if (over && mouse.leftButton.wasPressedThisFrame)
        {
            AudioView.Click();
            Clicked?.Invoke();
        }
    }

    private bool IsOver(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null || icon.sprite == null) return false;
        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = icon.bounds;
        return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }
}
