using UnityEngine;

public class WeatherView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer rain;
    [SerializeField] private SpriteRenderer wind;
    [SerializeField] private SpriteRenderer fog;

    [Tooltip("Поезд: окно в тумане держится над тем, что ближе всего к камере")]
    [SerializeField] private TrainView train;

    [Tooltip("Сколько секунд погода набегает и уходит")]
    [SerializeField] private float fadeTime = 0.6f;

    [Header("Окно в тумане")]
    [Tooltip("Во сколько раз окно больше половины вагона (овал должен накрыть углы)")]
    [SerializeField] private float windowScale = 1.25f;
    [Tooltip("Запас вокруг вагона, пиксели")]
    [SerializeField] private Vector2 windowPadding = new Vector2(8f, 8f);
    [Tooltip("Как быстро окно перетекает к другому вагону")]
    [SerializeField] private float flowSpeed = 5f;

    private static readonly int WindowSizeId = Shader.PropertyToID("_WindowSize");

    private MaterialPropertyBlock block;
    private Vector2 windowCenter;
    private Vector2 windowRadius;
    private bool hasWindow;
    private SpriteRenderer[] layers;
    private float[] baseAlpha;
    private readonly float[] current = new float[3];
    private readonly bool[] wanted = new bool[3];

    private void Awake()
    {
        layers = new[] { rain, wind, fog };
        baseAlpha = new float[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            baseAlpha[i] = layers[i].color.a;
            layers[i].gameObject.SetActive(false);
        }
    }

    public void Show(bool rainOn, bool windOn, bool fogOn)
    {
        wanted[0] = rainOn;
        wanted[1] = windOn;
        wanted[2] = fogOn;
    }

    private void LateUpdate()
    {
        float step = fadeTime > 0f ? Time.deltaTime / fadeTime : 1f;
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer == null) continue;

            current[i] = Mathf.MoveTowards(current[i], wanted[i] ? 1f : 0f, step);
            bool visible = current[i] > 0f;
            if (layer.gameObject.activeSelf != visible) layer.gameObject.SetActive(visible);
            if (!visible) continue;

            var color = layer.color;
            color.a = baseAlpha[i] * current[i];
            layer.color = color;
        }

        if (fog == null) return;
        if (wanted[2]) FollowFocus();
        else if (current[2] <= 0f) hasWindow = false;
    }

    private void FollowFocus()
    {
        var focus = train != null ? train.FogFocusBounds : null;
        if (!focus.HasValue) return;

        Vector2 targetCenter = focus.Value.center;
        Vector2 targetRadius = (Vector2)focus.Value.extents * windowScale + windowPadding;
        if (!hasWindow)
        {
            windowCenter = targetCenter;
            windowRadius = targetRadius;
            hasWindow = true;
        }

        float k = 1f - Mathf.Exp(-flowSpeed * Time.deltaTime);
        windowCenter = Vector2.Lerp(windowCenter, targetCenter, k);
        windowRadius = Vector2.Lerp(windowRadius, targetRadius, k);

        fog.transform.position = new Vector3(Mathf.Round(windowCenter.x), Mathf.Round(windowCenter.y), fog.transform.position.z);
        if (block == null) block = new MaterialPropertyBlock();
        fog.GetPropertyBlock(block);
        block.SetVector(WindowSizeId, new Vector4(windowRadius.x, windowRadius.y, 0f, 0f));
        fog.SetPropertyBlock(block);
    }
}
