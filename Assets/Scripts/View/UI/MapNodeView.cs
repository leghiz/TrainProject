using System;
using UnityEngine;
using UnityEngine.UI;

public enum MapNodeState { Passed, Current, Available, Locked }

// Узел карты. Образец лежит в Canvas выключенным — код делает из него копии.
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Text label;

    [Header("Цвет фона по состоянию")]
    [SerializeField] private Color currentColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.65f, 0.65f, 0.65f);
    [SerializeField] private Color passedColor = new Color(0.45f, 0.45f, 0.45f);

    public event Action Clicked;

    private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());

    // Нет иконки (арт ещё не готов) — видно хотя бы название.
    public void SetStation(Sprite sprite, string name)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        if (label != null) label.text = name;
    }

    public void SetState(MapNodeState state)
    {
        button.interactable = state == MapNodeState.Available;
        if (background == null) return;
        switch (state)
        {
            case MapNodeState.Current: background.color = currentColor; break;
            case MapNodeState.Available: background.color = availableColor; break;
            case MapNodeState.Passed: background.color = passedColor; break;
            default: background.color = lockedColor; break;
        }
    }
}
