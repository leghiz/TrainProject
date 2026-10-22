using System;
using UnityEngine;

// Поезд доехал до станции: камера плавно встаёт к локомотиву и чуть приближается, экран темнеет —
// и уже на тёмном экране открывается станция (её фон того же цвета, так что шва не видно).
public class ArrivalView : MonoBehaviour
{
    [SerializeField] private CameraRig cameraRig;
    [Tooltip("Затемнение на весь экран. Цвет и прозрачность — как у фона экрана станции")]
    [SerializeField] private SpriteRenderer fade;
    [Tooltip("Через сколько секунд после прибытия экран начинает темнеть")]
    [SerializeField] private float fadeDelay = 0.4f;
    [Tooltip("За сколько секунд экран темнеет")]
    [SerializeField] private float fadeTime = 0.8f;

    private Color fadeColor;
    private float time;
    private Action done;

    private void Awake()
    {
        fadeColor = fade.color;
        fade.gameObject.SetActive(false);
    }

    public void Play(Action onDone)
    {
        done = onDone;
        time = 0f;
        if (cameraRig != null) cameraRig.SetArrival(true);
        fade.gameObject.SetActive(true);
        Apply(0f);
    }

    public void Stop()
    {
        done = null;
        fade.gameObject.SetActive(false);
        if (cameraRig != null) cameraRig.SetArrival(false);
    }

    private void Update()
    {
        if (done == null) return;
        time += Time.unscaledDeltaTime;
        float t = fadeTime > 0f ? Mathf.Clamp01((time - fadeDelay) / fadeTime) : time >= fadeDelay ? 1f : 0f;
        Apply(t * t * (3f - 2f * t));
        if (time < fadeDelay + fadeTime) return;

        var action = done;
        Stop();
        action();
    }

    private void Apply(float share)
    {
        var color = fadeColor;
        color.a *= share;
        fade.color = color;
    }
}
