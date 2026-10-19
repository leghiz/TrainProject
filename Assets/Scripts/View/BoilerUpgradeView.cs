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
    private bool blocked;
    private bool wanted = true;
    private bool suppressed;
    
    public void SetBlocked(bool value) => blocked = value;

    // Пока открыто меню коптильни, кнопка прячется: она лежит там же, где полоска дров.
    public void SetSuppressed(bool value)
    {
        suppressed = value;
        gameObject.SetActive(wanted && !suppressed);
    }

    public void Set(string cost, bool canUpgrade)
    {
        wanted = true;
        gameObject.SetActive(!suppressed);
        if (costText != null) costText.text = cost;
        interactable = canUpgrade;
        background.color = canUpgrade ? availableColor : unavailableColor;
    }

    public void Hide()
    {
        wanted = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (!interactable || blocked || mouse == null || cam == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (PointerOverUI.Check()) return;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = background.bounds;
        if (world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y)
            Clicked?.Invoke();
    }
}
