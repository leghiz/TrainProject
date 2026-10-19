using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoilerUpgradeView : MonoBehaviour
{
    [Tooltip("Фон кнопки: по его размеру ловится клик")]
    [SerializeField] private SpriteRenderer background;
    [Tooltip("Цена, например «↑ 10»")]
    [SerializeField] private TextMesh costText;

    [SerializeField] private Color availableColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] private Color unavailableColor = new Color(0.55f, 0.55f, 0.55f);

    public event Action Clicked;

    private bool interactable;

    public void Set(string cost, bool canUpgrade)
    {
        gameObject.SetActive(true);
        if (costText != null) costText.text = cost;
        interactable = canUpgrade;
        background.color = canUpgrade ? availableColor : unavailableColor;
    }

    public void Hide() => gameObject.SetActive(false);

    private void Update()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (!interactable || mouse == null || cam == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (PointerOverUI.Check()) return;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = background.bounds;
        if (world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y)
            Clicked?.Invoke();
    }
}
