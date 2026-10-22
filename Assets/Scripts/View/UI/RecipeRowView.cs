using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Строка меню верстака: цена слева (иконки ресурсов и рамки с числами), стрелка, станок справа.
// Цену и картинку берёт из выбранного станка; рамки под каждый нужный ресурс копируются из образцов в Play.
public class RecipeRowView : MonoBehaviour
{
    [Tooltip("Какой станок крафтит эта строка. Он должен быть в списке recipes у Locomotive.asset")]
    [SerializeField, Expandable] private MachineConfig recipe;

    [Header("Куда подставлять данные станка")]
    [Tooltip("Картинка станка: на неё наводятся и жмут")]
    [SerializeField] private SpriteRenderer icon;
    [Tooltip("Стрелка между ценой и станком: от неё влево раскладывается цена")]
    [SerializeField] private Transform pointer;

    [Header("Цена: образцы, копируются под каждый нужный ресурс")]
    [SerializeField] private SpriteRenderer costIcon;
    [SerializeField] private SpriteRenderer costBox;
    [SerializeField] private TMP_Text costCount;
    [SerializeField] private Sprite woodSprite;
    [SerializeField] private Sprite scrapSprite;
    [SerializeField] private Sprite ingotSprite;
    [Tooltip("От стрелки до ближней рамки цены, пиксели")]
    [SerializeField] private float firstBoxOffset = 7.7f;
    [Tooltip("Между рамками цены, пиксели")]
    [SerializeField] private float boxSpacing = 10.5f;
    [Tooltip("От самой левой рамки до ближней иконки ресурса, пиксели")]
    [SerializeField] private float iconGap = 12.5f;
    [Tooltip("Между иконками ресурсов, пиксели")]
    [SerializeField] private float iconSpacing = 12.8f;

    [Header("Нельзя скрафтить")]
    [SerializeField] private Color lockedTint = new Color(1f, 1f, 1f, 0.35f);

    public MachineConfig Recipe => recipe;
    public Vector3 IconCenter => icon.bounds.center;
    public Vector2 IconSize => icon.bounds.size;

    public event Action Clicked;
    public event Action Hovered;
    public event Action Unhovered;

    private readonly List<(SpriteRenderer icon, SpriteRenderer box, TMP_Text count)> costs =
        new List<(SpriteRenderer, SpriteRenderer, TMP_Text)>();
    private float countOffset;
    private bool interactable = true;
    private bool hovered;

    // Обучение: строку обводят, пока её не нажмут.
    public void SetHighlighted(bool value)
    {
        var outline = icon != null ? icon.GetComponent<HoverOutline>() : null;
        if (outline != null) outline.Pinned = value;
    }

    public void Refresh()
    {
        if (recipe == null || icon == null) return;
        icon.sprite = recipe.sprite;
        BuildCosts();
        ApplyTint();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        ApplyTint();
    }

    private void BuildCosts()
    {
        if (costIcon == null || costBox == null || costCount == null || pointer == null) return;

        var needed = new List<(Sprite sprite, int amount)>();
        if (recipe.craftIngots > 0) needed.Add((ingotSprite, recipe.craftIngots));
        if (recipe.craftScrap > 0) needed.Add((scrapSprite, recipe.craftScrap));
        if (recipe.craftWood > 0) needed.Add((woodSprite, recipe.craftWood));

        if (costs.Count == 0)
        {
            countOffset = costCount.transform.localPosition.x - costBox.transform.localPosition.x;
            costs.Add((costIcon, costBox, costCount));
        }
        while (costs.Count < needed.Count) costs.Add((Copy(costIcon), Copy(costBox), Copy(costCount)));

        float leftmostBox = pointer.localPosition.x - firstBoxOffset - (needed.Count - 1) * boxSpacing;
        for (int i = 0; i < costs.Count; i++)
        {
            var (costSprite, box, count) = costs[i];
            bool used = i < needed.Count;
            costSprite.gameObject.SetActive(used);
            box.gameObject.SetActive(used);
            count.gameObject.SetActive(used);
            if (!used) continue;

            float boxX = leftmostBox + i * boxSpacing;
            float iconX = leftmostBox - iconGap - (needed.Count - 1 - i) * iconSpacing;
            Move(box.transform, boxX);
            Move(count.transform, boxX + countOffset);
            Move(costSprite.transform, iconX);
            costSprite.sprite = needed[i].sprite;
            count.text = needed[i].amount.ToString();
        }
    }

    private static T Copy<T>(T template) where T : Component =>
        Instantiate(template.gameObject, template.transform.parent).GetComponent<T>();

    private static void Move(Transform target, float x)
    {
        var position = target.localPosition;
        position.x = x;
        target.localPosition = position;
    }

    private void ApplyTint()
    {
        var tint = interactable ? Color.white : lockedTint;
        if (icon != null) icon.color = tint;
        foreach (var (costSprite, box, count) in costs)
        {
            costSprite.color = tint;
            count.alpha = tint.a;
        }
    }

    private void Update()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null || icon == null) return;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var bounds = icon.bounds;
        bool over = world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y
                    && !PointerOverUI.Check();
        if (over != hovered)
        {
            hovered = over;
            if (over) Hovered?.Invoke();
            else Unhovered?.Invoke();
        }

        if (over && interactable && mouse.leftButton.wasPressedThisFrame && TutorialFocus.Allows(transform))
        {
            AudioView.Click();
            Clicked?.Invoke();
        }
    }

    private void OnDisable()
    {
        if (!hovered) return;
        hovered = false;
        Unhovered?.Invoke();
    }
}
