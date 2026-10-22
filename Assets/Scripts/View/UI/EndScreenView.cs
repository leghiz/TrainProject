using UnityEngine;
using UnityEngine.InputSystem;

// Конец забега (доехали до Конечной или локомотив разрушен): по центру экрана плашка «Thanks for playing», под ней «R».
// R на клавиатуре или клик по плашке с R — игра заново.
public class EndScreenView : MonoBehaviour
{
    [Tooltip("Всё содержимое экрана: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Что раздувается при появлении")]
    [SerializeField] private Transform pop;
    [SerializeField] private Game game;
    [Tooltip("Плашка с R: клик по ней — тоже заново")]
    [SerializeField] private SpriteRenderer restartPanel;

    [Header("Появление")]
    [SerializeField] private float showTime = 0.25f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 1.8f;

    private float time;

    private void Awake() => root.SetActive(false);

    public void Show()
    {
        if (root.activeSelf) return;
        root.SetActive(true);
        time = 0f;
        PointerOverUI.Modal = transform;
        Animate();
    }

    private void Update()
    {
        if (!root.activeSelf) return;
        time += Time.unscaledDeltaTime;
        Animate();

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame && IsOver(mouse);
        if ((keyboard != null && keyboard.rKey.wasPressedThisFrame) || clicked) Restart();
    }

    private void Restart()
    {
        if (PointerOverUI.Modal == transform) PointerOverUI.Modal = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (game != null) game.Restart();
    }

    private void OnDestroy()
    {
        if (PointerOverUI.Modal == transform) PointerOverUI.Modal = null;
    }

    private bool IsOver(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null || restartPanel == null) return false;
        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = restartPanel.bounds;
        return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }

    private void Animate()
    {
        if (pop == null) return;
        float t = showTime > 0f ? Mathf.Clamp01(time / showTime) : 1f;
        float back = t - 1f;
        float scale = Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
        pop.localScale = new Vector3(scale, scale, 1f);
    }
}
