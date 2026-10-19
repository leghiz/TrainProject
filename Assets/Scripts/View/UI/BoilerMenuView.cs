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
    [SerializeField] private WorldButton addButton;
    [Tooltip("Что тускнеет, когда дров положить нельзя (нет дерева или бак полон)")]
    [SerializeField] private SpriteRenderer[] addButtonParts;
    [Tooltip("Плюсик на кнопке: тускнеет вместе с ней")]
    [SerializeField] private TMP_Text addLabel;
    [SerializeField] private Color disabledTint = new Color(1f, 1f, 1f, 0.4f);

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
    public event Action CloseClicked;

    public bool IsOpen => root.activeSelf;
    public int WoodPerClick => woodPerClick;

    private Color[] partColors;
    private Color tractionColor;
    private float time;

    private void Awake()
    {
        if (addButton != null) addButton.Clicked += () => AddClicked?.Invoke();
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
        if (woodCost != null) woodCost.text = woodPerClick.ToString();
        Place();
        Animate();
    }

    public void Hide() => root.SetActive(false);

    public void SetFuel(int loaded, int capacity)
    {
        if (fuelFill == null) return;
        float share = capacity > 0 ? Mathf.Clamp01((float)loaded / capacity) : 0f;
        int width = Mathf.Max(fuelWidth, 1);
        fuelFill.localScale = new Vector3(Mathf.Round(share * width) / width, 1f, 1f);
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
