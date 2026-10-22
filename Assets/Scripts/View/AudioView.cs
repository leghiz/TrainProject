using System;
using UnityEngine;

// Звук игры. Каждый звук — свой AudioSource на дочернем объекте префаба Audio: клип, высота
// и группа микшера (GameMixer: петли — Ambience, разовые — SFX) настраиваются на нём.
// Громкость каждого звука — ползунком здесь (работает и прямо в Play), громкость самих AudioSource оставь 1.
// Петли: стук колёс (пока поезд едет — плавно нарастает и затихает, на скорости чуть выше),
// ветер (всегда тихо, в ветреную погоду громко) и дождь (только в дождь).
// Разовые: клик, взять и поставить (станок, ящик, вагон) — высота каждый раз чуть разная, для разнообразия.
public class AudioView : MonoBehaviour
{
    [Serializable]
    public class Loop
    {
        public AudioSource source;
        [Tooltip("Громкость, когда звук слышно в полную силу")]
        [Range(0f, 1f)] public float volume = 0.5f;
        [Tooltip("За сколько секунд звук плавно нарастает и затихает")]
        public float fadeTime = 1.5f;
        [NonSerialized] public float pitch;
        [NonSerialized] public float level;
    }

    [Serializable]
    public class Shot
    {
        public AudioSource source;
        [Range(0f, 1f)] public float volume = 0.6f;
        [Tooltip("Разброс высоты: каждый раз случайно в пределах высоты AudioSource плюс-минус это")]
        [Range(0f, 0.5f)] public float pitchSpread = 0.08f;
        [NonSerialized] public float pitch;
    }

    [Header("Стук колёс")]
    [SerializeField] private Loop rails = new Loop();
    [Tooltip("Насколько стук становится выше на полной скорости")]
    [Range(0f, 0.5f)] [SerializeField] private float railsPitchBySpeed = 0.12f;

    [Header("Ветер")]
    [SerializeField] private Loop wind = new Loop();
    [Tooltip("Громкость ветра в обычную погоду; в ветер — громкость выше")]
    [Range(0f, 1f)] [SerializeField] private float calmWindVolume = 0.12f;

    [Header("Дождь")]
    [SerializeField] private Loop rain = new Loop();

    [Header("Разовые")]
    [SerializeField] private Shot click = new Shot();
    [Tooltip("Взять станок, ящик или вагон")]
    [SerializeField] private Shot pickup = new Shot();
    [Tooltip("Поставить станок, ящик или вагон")]
    [SerializeField] private Shot place = new Shot();

    private static AudioView instance;

    private bool raining;
    private bool windy;
    private bool travelling;

    public static void Click() => PlayShot(view => view.click);

    public static void Pickup() => PlayShot(view => view.pickup);

    public static void Place() => PlayShot(view => view.place);

    private static void PlayShot(Func<AudioView, Shot> pick)
    {
        if (instance != null) Play(pick(instance));
    }

    private void Awake()
    {
        instance = this;
        StartLoop(rails);
        StartLoop(wind);
        StartLoop(rain);
        Prepare(click);
        Prepare(pickup);
        Prepare(place);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void SetWeather(bool rainy, bool windyWeather)
    {
        raining = rainy;
        windy = windyWeather;
    }

    // Стук колёс слышно только в пути: доехали до станции — он затихает сразу, пока поезд тормозит.
    public void SetTravelling(bool value) => travelling = value;

    // Петля играет всегда, громкость решает, слышно ли её. Старт со случайного места — чтобы не звучало одинаково.
    private static void StartLoop(Loop loop)
    {
        var source = loop.source;
        if (source == null) return;
        loop.pitch = source.pitch;
        source.loop = true;
        source.volume = 0f;
        if (source.clip == null) return;
        source.time = UnityEngine.Random.Range(0f, source.clip.length);
        source.Play();
    }

    private static void Prepare(Shot shot)
    {
        if (shot.source != null) shot.pitch = shot.source.pitch;
    }

    private static void Play(Shot shot)
    {
        var source = shot.source;
        if (source == null || source.clip == null) return;
        source.pitch = shot.pitch + UnityEngine.Random.Range(-shot.pitchSpread, shot.pitchSpread);
        source.PlayOneShot(source.clip, shot.volume);
    }

    private void Update()
    {
        var motion = TrainMotionView.Current;
        bool moving = motion != null && motion.IsMoving;
        float speed = motion != null ? Mathf.Clamp01(motion.SpeedShare) : 0f;

        Fade(rails, moving && travelling, 0f);
        // Стоим (станция, карта) и стук затих — ставим его на настоящую паузу, в пути снова играет.
        if (rails.source != null && rails.source.clip != null)
        {
            if (rails.level <= 0f && rails.source.isPlaying) rails.source.Pause();
            else if (rails.level > 0f && !rails.source.isPlaying) rails.source.UnPause();
        }
        if (rails.source != null) rails.source.pitch = rails.pitch + railsPitchBySpeed * speed;
        // Ветер только в пути: тихо в обычную погоду, громко в ветер. На станции и карте молчит.
        if (wind.source != null)
        {
            float windTarget = !travelling ? 0f : windy ? wind.volume : calmWindVolume;
            float windStep = (wind.fadeTime > 0f ? Time.unscaledDeltaTime / wind.fadeTime : 1f) * Mathf.Max(wind.volume, 0.01f);
            wind.source.volume = Mathf.MoveTowards(wind.source.volume, windTarget, windStep);
        }
        Fade(rain, raining, 0f);
    }

    private static void Fade(Loop loop, bool on, float quietVolume)
    {
        if (loop.source == null) return;
        loop.level = Mathf.MoveTowards(loop.level, on ? 1f : 0f, loop.fadeTime > 0f ? Time.unscaledDeltaTime / loop.fadeTime : 1f);
        loop.source.volume = Mathf.Lerp(quietVolume, loop.volume, loop.level);
    }
}
