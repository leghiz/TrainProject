using System;
using TMPro;
using UnityEngine;

// Награда за доставку на экране станции: посередине появляется ящик и улетает вверх, сверху падают станок и слиток.
// Станок — клик, и он в руке (ставим в вагон), слиток — клик, и он летит в счётчик слитков.
public class DeliveryRewardView : MonoBehaviour
{
    [Tooltip("Всё содержимое награды: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [SerializeField] private SpriteRenderer box;
    [SerializeField] private RewardItemView machine;
    [SerializeField] private RewardItemView ingot;
    [Tooltip("Сколько слитков: число под слитком. Можно оставить пустым")]
    [SerializeField] private TMP_Text ingotCount;
    [Tooltip("Куда летит слиток: число слитков в панели ресурсов (AllResources)")]
    [SerializeField] private Transform ingotTarget;

    [Header("Расстановка")]
    [Tooltip("Высота ящика и наград от центра экрана, пиксели")]
    [SerializeField] private float rowY = 4f;
    [Tooltip("Расстояние между станком и слитком, пиксели")]
    [SerializeField] private float spacing = 44f;

    [Header("Ящик")]
    [SerializeField] private float boxPopTime = 0.25f;
    [Tooltip("Сколько секунд ящик стоит посередине, прежде чем улететь")]
    [SerializeField] private float boxHold = 0.45f;
    [SerializeField] private float boxFlyTime = 0.35f;
    [Tooltip("Насколько высоко улетает ящик, пиксели")]
    [SerializeField] private float boxFlyHeight = 140f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 1.8f;
    [Tooltip("Через сколько секунд после взлёта ящика начинают падать награды")]
    [SerializeField] private float dropDelay = 0.1f;
    [Tooltip("Слиток падает на столько позже станка, секунды")]
    [SerializeField] private float stagger = 0.08f;

    public event Action MachineClicked;
    public event Action IngotArrived;

    public bool IsOpen => root.activeSelf;

    private Vector3 boxScale;
    private float boxTime = -1f;

    private void Awake()
    {
        if (box != null) boxScale = box.transform.localScale;
        if (machine != null) machine.Clicked += () => MachineClicked?.Invoke();
        if (ingot != null)
        {
            ingot.Clicked += FlyIngot;
            ingot.Arrived += () => IngotArrived?.Invoke();
        }
        root.SetActive(false);
    }

    // withBox — с ящиком, который улетает вверх (первый показ); без него награды просто стоят на местах.
    public void Play(Sprite boxSprite, Sprite machineSprite, Material machineMaterial, int ingots,
                     bool withBox, bool machineLeft, bool ingotLeft)
    {
        root.SetActive(true);
        if (ingotCount != null) ingotCount.text = ingots.ToString();

        bool both = machineLeft && ingotLeft;
        var machineHome = new Vector3(both ? -spacing / 2f : 0f, rowY, 0f);
        var ingotHome = new Vector3(both ? spacing / 2f : 0f, rowY, 0f);
        float dropAt = withBox ? boxPopTime + boxHold + dropDelay : 0f;

        if (box != null)
        {
            box.gameObject.SetActive(withBox);
            if (withBox)
            {
                box.sprite = boxSprite;
                box.transform.localPosition = new Vector3(0f, rowY, 0f);
                boxTime = 0f;
                AnimateBox();
            }
        }

        if (machine != null)
        {
            if (!machineLeft) machine.Hide();
            else if (withBox) machine.Drop(machineSprite, machineMaterial, machineHome, dropAt);
            else machine.Place(machineSprite, machineMaterial, machineHome);
        }
        if (ingot != null)
        {
            if (!ingotLeft) ingot.Hide();
            else if (withBox) ingot.Drop(null, null, ingotHome, dropAt + stagger);
            else ingot.Place(null, null, ingotHome);
        }
    }

    public void Hide()
    {
        boxTime = -1f;
        root.SetActive(false);
    }

    private void FlyIngot()
    {
        AudioView.Click();
        if (ingotTarget != null) ingot.FlyTo(ingotTarget.position);
        else ingot.FlyTo(ingot.transform.position);
    }

    private void LateUpdate()
    {
        if (!root.activeSelf || boxTime < 0f) return;
        boxTime += Time.unscaledDeltaTime;
        AnimateBox();
    }

    private void AnimateBox()
    {
        if (box == null) return;
        if (boxTime < boxPopTime + boxHold)
        {
            float t = boxPopTime > 0f ? Mathf.Clamp01(boxTime / boxPopTime) : 1f;
            float back = t - 1f;
            float scale = Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
            box.transform.localScale = boxScale * scale;
            SetBoxAlpha(1f);
            return;
        }
        float fly = boxFlyTime > 0f ? Mathf.Clamp01((boxTime - boxPopTime - boxHold) / boxFlyTime) : 1f;
        box.transform.localScale = boxScale;
        box.transform.localPosition = new Vector3(0f, Mathf.Round(rowY + boxFlyHeight * fly * fly), 0f);
        SetBoxAlpha(1f - fly);
        if (fly < 1f) return;
        boxTime = -1f;
        box.gameObject.SetActive(false);
    }

    private void SetBoxAlpha(float alpha)
    {
        var color = box.color;
        color.a = alpha;
        box.color = color;
    }
}
