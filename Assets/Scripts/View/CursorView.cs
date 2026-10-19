using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public class CursorView : MonoBehaviour
{
    [Tooltip("Скорость вращения, градусов в секунду. Размер — это Scale объекта")]
    [SerializeField] private float spinSpeed = 120f;
    [Tooltip("Шаг поворота в градусах: 0 — плавно, 45 — рывками")]
    [SerializeField] private float spinStep;

    private float angle;
    private Vector3 baseScale;

    private void Awake() => baseScale = transform.localScale;

    private void OnEnable() => Cursor.visible = false;

    private void OnDisable() => Cursor.visible = true;

    private void OnApplicationFocus(bool focus)
    {
        if (focus && isActiveAndEnabled) Cursor.visible = false;
    }

    private void LateUpdate()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        transform.position = new Vector3(world.x, world.y, 0f);

        angle = Mathf.Repeat(angle - spinSpeed * Time.unscaledDeltaTime, 360f);
        float shown = spinStep > 0f ? Mathf.Round(angle / spinStep) * spinStep : angle;
        transform.rotation = Quaternion.Euler(0f, 0f, shown);
        transform.localScale = baseScale * CameraRig.Zoom;
    }
}
