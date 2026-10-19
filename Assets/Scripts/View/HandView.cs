using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer ghost;

    [Header("Ящик в руке")]
    [Tooltip("Размер клетки вагона, пиксели: на столько клеток ящик сдвигается от мышки")]
    [SerializeField] private float cellSize = 16f;
    [Tooltip("Чем рисовать ящик без картинки: растягивается на его клетки")]
    [SerializeField] private Sprite blankSprite;

    public event Action Released;
    public event Action CancelPressed;
    public event Action RightReleased;

    private Vector3 offset;

    public void Show(Sprite sprite)
    {
        offset = Vector3.zero;
        ghost.transform.localScale = Vector3.one;
        ghost.sprite = sprite;
        ghost.gameObject.SetActive(sprite != null);
    }

    // Предмет на несколько клеток: cellOffset — где его центр относительно мышки, в клетках.
    public void ShowArea(Sprite sprite, Vector2 cells, Vector2 cellOffset)
    {
        offset = cellOffset * cellSize;
        if (sprite != null)
        {
            ghost.sprite = sprite;
            ghost.transform.localScale = Vector3.one;
        }
        else
        {
            ghost.sprite = blankSprite;
            var size = blankSprite != null ? (Vector2)blankSprite.bounds.size : Vector2.one;
            ghost.transform.localScale = new Vector3(cells.x * cellSize / size.x, cells.y * cellSize / size.y, 1f);
        }
        ghost.gameObject.SetActive(ghost.sprite != null);
    }

    public void Hide() => ghost.gameObject.SetActive(false);

    private void LateUpdate()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        ghost.transform.position = new Vector3(world.x, world.y, 0f) + offset;

        if (mouse.rightButton.wasPressedThisFrame) CancelPressed?.Invoke();
        if (mouse.rightButton.wasReleasedThisFrame) RightReleased?.Invoke();
        if (mouse.leftButton.wasReleasedThisFrame) Released?.Invoke();
    }
}
