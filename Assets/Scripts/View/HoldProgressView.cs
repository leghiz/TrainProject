using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public class HoldProgressView : MonoBehaviour
{
    [Tooltip("Отсюда берётся удержание: Game.Dismantle")]
    [SerializeField] private Game game;
    [SerializeField] private SpriteRenderer circle;
    [Tooltip("Сдвиг кружка от мышки, пиксели. Для кружка нечётного размера дробная часть .5 — тогда он ровно по пикселям")]
    [SerializeField] private Vector2 offset = new Vector2(7.5f, 6.5f);
    [Tooltip("Сколько секунд кружок проявляется и гаснет")]
    [SerializeField] private float fadeTime = 0.08f;
    [Tooltip("Насколько кружок раздувается, когда заполнился")]
    [SerializeField] private float donePop = 1.4f;

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");

    private MaterialPropertyBlock block;
    private Vector3 baseScale;
    private float alpha;
    private float shown;
    private float pop;
    private bool wasActive;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        baseScale = circle.transform.localScale;
        circle.enabled = false;
    }

    private void LateUpdate()
    {
        var hold = game != null ? game.Dismantle : null;
        bool active = hold != null && hold.IsActive;
        if (active) shown = hold.Progress;
        if (wasActive && !active && shown > 0.9f)
        {
            shown = 1f;
            pop = 1f;
        }
        wasActive = active;

        float step = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
        alpha = Mathf.MoveTowards(alpha, active ? 1f : 0f, step * (pop > 0f ? 0.5f : 1f));
        pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime / 0.15f);

        circle.enabled = alpha > 0f;
        if (!circle.enabled) return;

        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse != null && cam != null)
        {
            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            float zoom = CameraRig.Zoom;
            transform.position = new Vector3(Mathf.Round(world.x) + offset.x * zoom, Mathf.Round(world.y) + offset.y * zoom, 0f);
        }

        circle.GetPropertyBlock(block);
        block.SetFloat(ProgressId, shown);
        circle.SetPropertyBlock(block);

        var color = circle.color;
        color.a = alpha;
        circle.color = color;
        circle.transform.localScale = baseScale * Mathf.Lerp(1f, donePop, pop) * CameraRig.Zoom;
    }
}
