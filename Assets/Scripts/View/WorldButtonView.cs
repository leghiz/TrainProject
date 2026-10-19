using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Кнопка в мире (спрайт + текст): лежит в сцене, геймдиз оформляет её как хочет.
// Точка самого объекта встаёт туда, куда её ставит код (например, над ящиком).
public class WorldButtonView : MonoBehaviour
{
    [Tooltip("Фон кнопки: по его размеру ловится клик")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private TextMesh label;

    [SerializeField] private Color availableColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] private Color unavailableColor = new Color(0.55f, 0.55f, 0.55f);

    public event Action Clicked;
    // Нажали мимо кнопки — обычно это значит «передумал».
    public event Action PressedOutside;

    private bool interactable;

    public void ShowAt(Vector3 point, string text, bool canClick)
    {
        gameObject.SetActive(true);
        transform.position = new Vector3(Mathf.Round(point.x), Mathf.Round(point.y), 0f);
        if (label != null) label.text = text;
        SetInteractable(canClick);
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        background.color = value ? availableColor : unavailableColor;
    }

    public void Hide() => gameObject.SetActive(false);

    private void Update()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null || !mouse.leftButton.wasPressedThisFrame || PointerOverUI.Check()) return;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = background.bounds;
        bool inside = world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
        if (!inside) PressedOutside?.Invoke();
        else if (interactable) Clicked?.Invoke();
    }
}
