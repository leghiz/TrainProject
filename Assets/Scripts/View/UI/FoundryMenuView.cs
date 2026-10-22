using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// Меню плавильни: висит слева от квадратика её тултипа (что плавится и сколько осталось).
// Сверху — сколько лома лежит в плавильне (растёт от кнопки, тает по мере выплавки слитков), по центру лом, снизу кнопка «загрузить». Закрывается ПКМ или Esc.
// Пока мышь над меню, вагоны под ним не наводятся и не нажимаются.
[DefaultExecutionOrder(-50)]
public class FoundryMenuView : MonoBehaviour
{
    [Tooltip("Всё содержимое меню: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Центр раздувания при появлении: квадратик тултипа")]
    [SerializeField] private Transform pop;
    [Tooltip("За чем следует меню: рамка квадратика в тултипе станка (MachineTooltip → IconFrame)")]
    [SerializeField] private Transform anchor;
    [Tooltip("Сдвиг меню от центра рамки, пиксели")]
    [SerializeField] private Vector2 anchorOffset;

    [Header("Лом")]
    [Tooltip("Сколько лома кладёт одно нажатие кнопки")]
    [SerializeField, Min(1)] private int scrapPerClick = 1;
    [Tooltip("Число в рамке над ломом: сколько лома загружено в плавильню")]
    [FormerlySerializedAs("scrapCost")]
    [SerializeField] private TMP_Text loadedCount;
    [SerializeField] private WorldButton addButton;
    [Tooltip("Что тускнеет, когда лома положить нельзя (нет лома или плавильня полна)")]
    [SerializeField] private SpriteRenderer[] addButtonParts;
    [SerializeField] private Color disabledTint = new Color(1f, 1f, 1f, 0.4f);

    [Header("Появление")]
    [SerializeField] private float showTime = 0.18f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 1.8f;

    public event Action AddClicked;
    public event Action CloseClicked;

    public bool IsOpen => root.activeSelf;
    public int ScrapPerClick => scrapPerClick;

    private SpriteRenderer[] sprites;
    private Color[] partColors;
    private float time;

    private void Awake()
    {
        if (addButton != null) addButton.Clicked += () => AddClicked?.Invoke();
        sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        partColors = new Color[addButtonParts != null ? addButtonParts.Length : 0];
        for (int i = 0; i < partColors.Length; i++) partColors[i] = addButtonParts[i] != null ? addButtonParts[i].color : Color.white;
        root.SetActive(false);
    }

    private void OnDisable() => Unblock();

    public void Show()
    {
        root.SetActive(true);
        time = 0f;
        Place();
        Animate();
    }

    public void Hide()
    {
        root.SetActive(false);
        Unblock();
    }

    public void SetLoaded(int value)
    {
        if (loadedCount != null) loadedCount.text = value.ToString();
    }

    public void SetCanAdd(bool value)
    {
        if (addButton != null) addButton.Interactable = value;
        for (int i = 0; i < partColors.Length; i++)
        {
            if (addButtonParts[i] == null) continue;
            addButtonParts[i].color = value ? partColors[i] : partColors[i] * disabledTint;
        }
    }

    // Раньше всех в кадре: вагоны уже знают, что мышь над меню, и не ловят клик под ним.
    private void Update()
    {
        if (!root.activeSelf) return;
        Place();
        var mouse = Mouse.current;
        var cam = Camera.main;
        bool over = mouse != null && cam != null && IsOver(cam.ScreenToWorldPoint(mouse.position.ReadValue()));
        if (over) PointerOverUI.Panel = transform;
        else Unblock();
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

    private bool IsOver(Vector2 world)
    {
        foreach (var sprite in sprites)
        {
            if (sprite == null || !sprite.gameObject.activeInHierarchy) continue;
            var bounds = sprite.bounds;
            if (world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y) return true;
        }
        return false;
    }

    private void Unblock()
    {
        if (PointerOverUI.Panel == transform) PointerOverUI.Panel = null;
    }

    private void Place()
    {
        if (anchor == null) return;
        transform.position = anchor.position + (Vector3)anchorOffset;
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
