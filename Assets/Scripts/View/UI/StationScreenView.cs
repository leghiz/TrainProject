using System;
using System.Collections.Generic;
using UnityEngine;

// Экран станции: вагоны в ряд по центру экрана, над каждым стрелка, внизу кнопка «уехать».
// Лежит под камерой (ZoomRoot), поэтому всегда в одном месте экрана. Пока открыт, поезд и меню спрятаны.
// Не влезают в ширину экрана — сначала сжимаются промежутки, потом ряд чуть уменьшается.
// Взяли вагон — остальные разлетаются, а снизу появляется наш состав, куда его вставить (StationTrainView).
public class StationScreenView : MonoBehaviour
{
    [Tooltip("Всё содержимое экрана: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Затемнение под экраном")]
    [SerializeField] private SpriteRenderer backdrop;
    [SerializeField] private float backdropFadeTime = 0.2f;
    [Tooltip("Что прятать, пока экран открыт: поезд, меню верстака и коптильни")]
    [SerializeField] private GameObject[] hideWhileOpen;

    [Header("Вагоны")]
    [Tooltip("Ряд вагонов: в нём раскладываются копии образца")]
    [SerializeField] private Transform offers;
    [Tooltip("Образец вагона (выключен, код делает из него копии)")]
    [SerializeField] private StationOfferView offerTemplate;
    [Tooltip("Высота, на которой стоят колёса вагонов, пиксели от центра экрана")]
    [SerializeField] private float rowBottom = -6f;
    [Tooltip("Промежуток между вагонами, пиксели")]
    [SerializeField] private float gap = 49f;
    [Tooltip("Меньше этого промежутки не сжимаются — дальше уменьшается весь ряд")]
    [SerializeField] private float minGap = 16f;
    [Tooltip("Отступ ряда от краёв экрана, пиксели")]
    [SerializeField] private float sideMargin = 12f;
    [Tooltip("Задержка появления каждого следующего вагона, секунды")]
    [SerializeField] private float stagger = 0.05f;
    [Tooltip("Чем рисовать ящик посылки без картинки")]
    [SerializeField] private Sprite blankSprite;

    [Header("Куда вставить взятый вагон")]
    [SerializeField] private StationTrainView train;

    [Header("Уехать")]
    [SerializeField] private WorldButton leaveButton;

    [Header("Награда за доставку")]
    [SerializeField] private DeliveryRewardView reward;

    public event Action LeaveClicked;

    public bool IsOpen => root.activeSelf;
    public Sprite BlankSprite => blankSprite;
    public StationTrainView Train => train;
    public DeliveryRewardView Reward => reward;

    private readonly List<StationOfferView> items = new List<StationOfferView>();
    private bool[] hiddenWasActive;
    private Color backdropColor;
    private float fade;
    private float delay = -1f;
    private Action afterDelay;

    private void Awake()
    {
        if (backdrop != null) backdropColor = backdrop.color;
        if (offerTemplate != null) offerTemplate.gameObject.SetActive(false);
        if (train != null) train.gameObject.SetActive(false);
        if (leaveButton != null) leaveButton.Clicked += () => LeaveClicked?.Invoke();
    }

    // darkAlready — экран уже затемнён прибытием: фон сразу тёмный, без своего проявления.
    public void Show(bool darkAlready = false)
    {
        if (!root.activeSelf)
        {
            fade = darkAlready ? 1f : 0f;
            HideOthers(true);
        }
        root.SetActive(true);
        PointerOverUI.Modal = transform;
    }

    public void Hide()
    {
        if (root.activeSelf) HideOthers(false);
        SetInserting(false);
        if (reward != null) reward.Hide();
        root.SetActive(false);
        afterDelay = null;
        if (PointerOverUI.Modal == transform) PointerOverUI.Modal = null;
        UiPalette.SetInverted(false);
    }

    private void OnDestroy()
    {
        if (PointerOverUI.Modal == transform) PointerOverUI.Modal = null;
        UiPalette.SetInverted(false);
    }

    private void HideOthers(bool hide)
    {
        if (hideWhileOpen == null) return;
        if (hide) hiddenWasActive = new bool[hideWhileOpen.Length];
        for (int i = 0; i < hideWhileOpen.Length; i++)
        {
            var target = hideWhileOpen[i];
            if (target == null) continue;
            if (hide)
            {
                hiddenWasActive[i] = target.activeSelf;
                target.SetActive(false);
            }
            else if (hiddenWasActive != null) target.SetActive(hiddenWasActive[i]);
        }
    }

    public void ClearOffers()
    {
        foreach (var item in items) Destroy(item.gameObject);
        items.Clear();
    }

    public StationOfferView AddOffer()
    {
        var item = Instantiate(offerTemplate, offers);
        item.gameObject.SetActive(true);
        items.Add(item);
        return item;
    }

    public float AppearDelay(int index) => index * stagger;

    // Взяли вагон: остальные разлетаются в стороны, сам он уходит в руку (его рисует StationTrainView).
    public void Scatter(StationOfferView picked)
    {
        foreach (var item in items)
        {
            if (item == picked) continue;
            item.FlyAway(item.transform.position.x - picked.transform.position.x);
        }
        items.Clear();
        Destroy(picked.gameObject);
    }

    // Пока не забрали награду за доставку, уехать нельзя.
    public void SetLeaveVisible(bool value)
    {
        if (leaveButton != null) leaveButton.gameObject.SetActive(value);
    }

    // Шаг «куда вставить»: кнопка «уехать» прячется, снизу виден состав.
    public void SetInserting(bool value)
    {
        if (leaveButton != null) leaveButton.gameObject.SetActive(!value);
        if (train == null) return;
        if (!value) train.Clear();
        train.gameObject.SetActive(value);
    }

    public void After(float seconds, Action action)
    {
        delay = seconds;
        afterDelay = action;
    }

    // Раскладывает вагоны в ряд по центру, колёсами на одну линию.
    public void Layout()
    {
        if (items.Count == 0) return;

        float widths = 0f;
        foreach (var item in items) widths += item.Width;
        int gaps = items.Count - 1;
        float available = VisibleWidth() - sideMargin * 2f;

        float space = gap;
        if (gaps > 0 && widths + gaps * space > available) space = Mathf.Max(minGap, (available - widths) / gaps);
        float total = widths + gaps * space;
        float scale = total > available && total > 0f ? available / total : 1f;
        offers.localScale = new Vector3(scale, scale, 1f);

        float x = -total / 2f;
        foreach (var item in items)
        {
            float center = x + item.Width / 2f;
            item.transform.localPosition = new Vector3(Mathf.Round(center), Mathf.Round(rowBottom / scale - item.Bottom), 0f);
            x += item.Width + space;
        }
    }

    // Ширина экрана в пикселях игры без зума: экран лежит под ZoomRoot и масштабируется вместе с ним.
    private float VisibleWidth()
    {
        var cam = Camera.main;
        if (cam == null) return 320f;
        float zoom = Mathf.Max(CameraRig.Zoom, 0.01f);
        return cam.orthographicSize / zoom * 2f * cam.aspect;
    }

    private void LateUpdate()
    {
        if (!root.activeSelf) return;

        if (afterDelay != null)
        {
            delay -= Time.unscaledDeltaTime;
            if (delay <= 0f)
            {
                var action = afterDelay;
                afterDelay = null;
                action();
            }
        }

        if (backdrop == null) return;
        fade = Mathf.MoveTowards(fade, 1f, backdropFadeTime > 0f ? Time.unscaledDeltaTime / backdropFadeTime : 1f);
        var color = backdropColor;
        color.a *= fade;
        backdrop.color = color;
    }
}
