using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer ghost;

    public event Action Released;
    public event Action CancelPressed;

    public void Show(Sprite sprite)
    {
        ghost.sprite = sprite;
        ghost.gameObject.SetActive(sprite != null);
    }

    public void Hide() => ghost.gameObject.SetActive(false);
    
    private void LateUpdate()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        ghost.transform.position = new Vector3(world.x, world.y, 0f);

        if (mouse.rightButton.wasPressedThisFrame) CancelPressed?.Invoke();
        if (mouse.leftButton.wasReleasedThisFrame) Released?.Invoke();
    }
}
