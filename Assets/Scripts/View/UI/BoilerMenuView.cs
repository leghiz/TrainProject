using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Меню коптильни висит в мире вокруг неё: сверху сколько дерева в баке и кнопка «+» (подкинуть дров),
// снизу — тяга: сколько клеток вагонов тянем из скольких. Закрывается ПКМ или Esc.
public class BoilerMenuView : MonoBehaviour
{
    [Tooltip("Всё содержимое меню: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Центр коптильни: вокруг него меню раздувается при появлении")]
    [SerializeField] private Transform pop;
    [Tooltip("Над чем висит меню: коптильня на локомотиве (BoilerSlot)")]
    [SerializeField] private Transform anchor;
    [Tooltip("Сдвиг центра меню от центра коптильни, пиксели")]
    [SerializeField] private Vector2 anchorOffset;

    [Header("Дрова")]
    [Tooltip("Сколько дерева кладёт одно нажатие «+»")]
    [SerializeField, Min(1)] private int woodPerClick = 1;
    [Tooltip("Число в рамке у иконки дерева")]
    [SerializeField] private TMP_Text woodCost;
    [Tooltip("Точка у левого края полоски: её масштаб по X = сколько дерева в баке")]
    [SerializeField] private Transform fuelFill;
    [Tooltip("Ширина полоски в пикселях: заполнение округляется до целого пикселя")]
    [SerializeField] private int fuelWidth = 32;
    [Tooltip("Как быстро полоска доезжает до нового значения: доля полной полоски в секунду")]
    [SerializeField] private float fuelSpeed = 1.5f;
    [SerializeField] private WorldButton addButton;
    [Tooltip("Что тускнеет, когда дров положить нельзя (нет дерева или бак полон)")]
    [SerializeField] private SpriteRenderer[] addButtonParts;
    [Tooltip("Плюсик на кнопке: тускнеет вместе с ней")]
    [SerializeField] private TMP_Text addLabel;
    [SerializeField] private Color disabledTint = new Color(1f, 1f, 1f, 0.4f);

    [Header("Улучшение коптильни")]
    [Tooltip("Ряд улучшения целиком: прячется, когда коптильня уже на последнем уровне")]
    [SerializeField] private GameObject upgradeGroup;
    [SerializeField] private WorldButton upgradeButton;
    [Tooltip("Цена улучшения в слитках")]
    [SerializeField] private TMP_Text upgradeCost;
    [Tooltip("Что тускнеет, когда слитков не хватает")]
    [SerializeField] private SpriteRenderer[] upgradeParts;
    [Tooltip("Плюсик на кнопке улучшения: тускнеет вместе с ней")]
    [SerializeField] private TMP_Text upgradeLabel;

    [Header("Тяга")]
    [SerializeField] private TMP_Text traction;
    [Tooltip("Цвет числа, когда вагонов больше, чем тянет коптильня (поезд едет медленнее)")]
    [SerializeField] private Color overloadedColor = new Color(1f, 0.3f, 0.25f, 1f);

    [Header("Появление")]
    [SerializeField] private float showTime = 0.18f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 1.8f;

    public event Action AddClicked;
    public event Action UpgradeClicked;
    public event Action CloseClicked;

    public bool IsOpen => root.activeSelf;
    public int WoodPerClick => woodPerClick;

    private Color[] partColors;
    private Color[] upgradeColors;
    private Color tractionColor;
    private float time;
    private float fuelShown = -1f;
    private float fuelTarget;

    private void Awake()
    {
        if (addButton != null) addButton.Clicked += () => AddClicked?.Invoke();
        if (upgradeButton != null) upgradeButton.Clicked += () => UpgradeClicked?.Invoke();
        upgradeColors = Colors(upgradeParts);
        if (addButtonParts != null)
        {
            partColors = new Color[addButtonParts.Length];
            for (int i = 0; i < addButtonParts.Length; i++) partColors[i] = addButtonParts[i] != null ? addButtonParts[i].color : Color.white;
        }
        if (traction != null) tractionColor = traction.color;
        root.SetActive(false);
    }

    public void Show()
    {
        root.SetActive(true);
        time = 0f;
        fuelShown = -1f;
        if (woodCost != null) woodCost.text = woodPerClick.ToString();
        Place();
        Animate();
    }

    public void Hide() => root.SetActive(false);

    public Transform AddButtonPoint => addButton != null ? addButton.transform : null;

    // Обучение: кнопку «подкинуть дров» обводят, пока её не нажмут.
    public void HighlightAdd(bool value)
    {
        var outline = addButton != null ? addButton.GetComponent<HoverOutline>() : null;
        if (outline != null) outline.Pinned = value;
    }

    // Полоска не прыгает, а плавно доезжает до нового значения, по целым пикселям.
    public void SetFuel(int loaded, int capacity)
    {
        fuelTarget = capacity > 0 ? Mathf.Clamp01((float)loaded / capacity) : 0f;
        if (fuelShown < 0f) fuelShown = fuelTarget;
        ApplyFuel();
    }

    private void ApplyFuel()
    {
        if (fuelFill == null || fuelShown < 0f) return;
        int width = Mathf.Max(fuelWidth, 1);
        fuelFill.localScale = new Vector3(Mathf.Round(fuelShown * width) / width, 1f, 1f);
    }

    public void SetCanAdd(bool value)
    {
        if (addButton != null) addButton.Interactable = value;
        if (addLabel != null) addLabel.alpha = value ? 1f : disabledTint.a;
        if (addButtonParts == null) return;
        for (int i = 0; i < addButtonParts.Length; i++)
        {
            if (addButtonParts[i] == null) continue;
            addButtonParts[i].color = value ? partColors[i] : partColors[i] * disabledTint;
        }
    }

    // available — есть ли куда улучшать; canAfford — хватает ли слитков.
    public void SetUpgrade(int cost, bool available, bool canAfford)
    {
        if (upgradeGroup != null) upgradeGroup.SetActive(available);
        if (!available) return;
        if (upgradeCost != null) upgradeCost.text = cost.ToString();
        if (upgradeButton != null) upgradeButton.Interactable = canAfford;
        if (upgradeLabel != null) upgradeLabel.alpha = canAfford ? 1f : disabledTint.a;
        if (upgradeParts == null) return;
        for (int i = 0; i < upgradeParts.Length; i++)
        {
            if (upgradeParts[i] == null) continue;
            upgradeParts[i].color = canAfford ? upgradeColors[i] : upgradeColors[i] * disabledTint;
        }
    }

    private static Color[] Colors(SpriteRenderer[] parts)
    {
        if (parts == null) return new Color[0];
        var colors = new Color[parts.Length];
        for (int i = 0; i < parts.Length; i++) colors[i] = parts[i] != null ? parts[i].color : Color.white;
        return colors;
    }

    public void SetTraction(float weight, int max)
    {
        if (traction == null) return;
        traction.text = weight.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "/" + max;
        traction.color = weight > max ? overloadedColor : tractionColor;
    }

    private void LateUpdate()
    {
        if (!root.activeSelf) return;

        time += Time.unscaledDeltaTime;
        Place();
        Animate();
        if (fuelShown >= 0f)
        {
            fuelShown = Mathf.MoveTowards(fuelShown, fuelTarget, fuelSpeed * Time.unscaledDeltaTime);
            ApplyFuel();
        }

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            CloseClicked?.Invoke();
    }

    private void Place()
    {
        if (anchor == null) return;
        var target = anchor.position + (Vector3)anchorOffset;
        transform.position = new Vector3(Mathf.Round(target.x), Mathf.Round(target.y), transform.position.z);
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
