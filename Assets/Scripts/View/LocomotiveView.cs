using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum LocomotiveSlot { Boiler, Workbench, Welder }

public class LocomotiveView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;

    [Tooltip("Точка у левого края полоски хп. Её масштаб по X = доля оставшегося хп")]
    [SerializeField] private Transform hpFill;

    [Header("Места станков (центры мест — двигать под спрайт локомотива)")]
    [SerializeField] private MachineView machinePrefab;
    [SerializeField] private Transform boilerSlot;
    [SerializeField] private Transform workbenchSlot;
    [SerializeField] private Transform welderSlot;

    [Tooltip("Размер одной клетки в единицах мира (при PPU = 1 это 16)")]
    [SerializeField] private float cellSize = 16f;

    [Header("Подсветка установки")]
    [SerializeField] private SpriteRenderer placementPreview;
    [SerializeField] private Color canPlaceColor = new Color(0.3f, 1f, 0.3f, 0.45f);
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0.3f, 0.3f, 0.45f);

    public event Action WelderSlotHovered;
    public event Action WelderSlotExited;
    public event Action WelderSlotClicked;

    public bool IsWelderSlotHovered { get; private set; }

    public void SetSprite(Sprite sprite) => body.sprite = sprite;

    public float Width => body.bounds.size.x;
    public float Height => body.bounds.size.y;

    public void SetHp(float normalized) => hpFill.localScale = new Vector3(normalized, 1f, 1f);

    public void ShowDestroyed() => gameObject.SetActive(false);
    
    public MachineView AddMachine(LocomotiveSlot slot) =>
        Instantiate(machinePrefab, SlotAnchor(slot).position, Quaternion.identity, transform);

    public void RemoveMachine(MachineView machine) => Destroy(machine.gameObject);

    public void ShowWelderPreview(bool canPlace)
    {
        placementPreview.gameObject.SetActive(true);
        placementPreview.color = canPlace ? canPlaceColor : cannotPlaceColor;
        placementPreview.transform.position = welderSlot.position;
        placementPreview.transform.localScale = new Vector3(cellSize, cellSize, 1f);
    }

    public void HidePlacementPreview() => placementPreview.gameObject.SetActive(false);

    private void Update()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 offset = world - (Vector2)welderSlot.position;
        bool hovered = Mathf.Abs(offset.x) <= cellSize / 2f && Mathf.Abs(offset.y) <= cellSize / 2f;

        if (hovered != IsWelderSlotHovered)
        {
            IsWelderSlotHovered = hovered;
            if (hovered) WelderSlotHovered?.Invoke();
            else WelderSlotExited?.Invoke();
        }

        if (hovered && mouse.leftButton.wasPressedThisFrame) WelderSlotClicked?.Invoke();
    }

    private Transform SlotAnchor(LocomotiveSlot slot)
    {
        switch (slot)
        {
            case LocomotiveSlot.Boiler: return boilerSlot;
            case LocomotiveSlot.Workbench: return workbenchSlot;
            default: return welderSlot;
        }
    }
}
