using UnityEngine;

public class SpawnPop : MonoBehaviour
{
    [SerializeField] private float duration = 0.22f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу перед тем, как встать")]
    [SerializeField] private float overshoot = 2.2f;

    private Vector3 baseScale;
    private float time;

    private void Awake() => baseScale = transform.localScale;

    private void OnEnable()
    {
        time = 0f;
        Apply();
    }

    private void LateUpdate()
    {
        if (time >= duration) return;
        time += Time.unscaledDeltaTime;
        Apply();
    }

    private void Apply()
    {
        float t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
        float back = t - 1f;
        float scale = Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
        transform.localScale = baseScale * scale;
    }
}
