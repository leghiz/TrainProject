using TMPro;
using UnityEngine;

public class MachineTooltipView : MonoBehaviour
{
    [Tooltip("Всё содержимое тултипа: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Центр станка: вокруг него тултип раздувается при появлении")]
    [SerializeField] private Transform pop;
    [Tooltip("Всё, что над станком: ставится над его верхним краем")]
    [SerializeField] private Transform above;
    [Tooltip("Панель свойства сбоку: ставится у правого края станка")]
    [SerializeField] private Transform side;
    [Tooltip("Зазор между верхним краем станка и стрелкой тултипа, пиксели")]
    [SerializeField] private float gap = 4f;
    [Tooltip("Высота плашки над станком, пиксели: если не влезает до верха экрана — плашка уходит под станок")]
    [SerializeField] private float aboveHeight = 30f;
    [Tooltip("Стрелка от квадратика к станку")]
    [SerializeField] private Transform pointer;

    [Header("Справа от станка (меню верстака)")]
    [Tooltip("Где стрелка, если плашка справа (от правого края станка), пиксели")]
    [SerializeField] private Vector2 besidePointer = new Vector2(2.5f, 0f);
    [Tooltip("Сдвиг группы с квадратиком, если плашка справа: квадратик по центру станка")]
    [SerializeField] private Vector2 besideResource = new Vector2(14f, -20.5f);
    [Tooltip("Сдвиг полоски хп, если плашка справа: под квадратиком")]
    [SerializeField] private Vector2 besideHp = new Vector2(14f, -19.5f);

    [Header("Ресурс")]
    [SerializeField] private GameObject resourceGroup;
    [SerializeField] private SpriteRenderer resourceIcon;
    [SerializeField] private Sprite woodIcon;
    [SerializeField] private Sprite scrapIcon;
    [SerializeField] private Sprite ingotIcon;
    [Tooltip("Иконка для чинящих станков (сварка): без заливки прогресса")]
    [SerializeField] private Sprite healingIcon;
    [Tooltip("Подложка заливки: у чинящих станков прячется")]
    [SerializeField] private GameObject progressBack;
    [Tooltip("Точка у нижнего края заливки: её масштаб по Y = сколько осталось до добычи")]
    [SerializeField] private Transform progressFill;
    [SerializeField] private int progressHeight = 15;

    [Header("Хп")]
    [SerializeField] private GameObject hpGroup;
    [Tooltip("Точка у левого края полоски: её масштаб по X = доля хп")]
    [SerializeField] private Transform hpFill;
    [SerializeField] private int hpWidth = 15;

    [Header("Свойство")]
    [SerializeField] private TMP_Text traitName;
    [SerializeField] private TMP_Text traitInfo;

    [Header("Появление")]
    [SerializeField] private float showTime = 0.2f;
    [SerializeField] private float hideTime = 0.1f;
    [SerializeField] private float startScale = 0.5f;
    [Tooltip("Пружинка: насколько размер перелетает единицу перед тем, как встать")]
    [SerializeField] private float overshoot = 1.7f;
    [Tooltip("На сколько пикселей выезжает снизу при появлении")]
    [SerializeField] private float rise = 6f;

    private Material defaultTraitMaterial;
    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private TMP_Text[] texts;
    private bool visible;
    private float progress;
    private Vector3 pointerPosition;
    private Quaternion pointerRotation;
    private Vector3 resourcePosition;
    private Vector3 hpPosition;

    private void Awake()
    {
        if (pointer != null)
        {
            pointerPosition = pointer.localPosition;
            pointerRotation = pointer.localRotation;
        }
        if (resourceGroup != null) resourcePosition = resourceGroup.transform.localPosition;
        if (hpGroup != null) hpPosition = hpGroup.transform.localPosition;

        if (traitName != null) defaultTraitMaterial = traitName.fontSharedMaterial;
        sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;
        texts = root.GetComponentsInChildren<TMP_Text>(true);
        root.SetActive(false);
    }

    public void Show(TooltipContent content, Vector3 anchor)
    {
        if (!visible)
        {
            visible = true;
            root.SetActive(true);
            Animate();
        }

        bool showIcon = content.HasResource || content.Heals;
        if (resourceGroup != null) resourceGroup.SetActive(showIcon);
        if (showIcon)
        {
            if (resourceIcon != null) resourceIcon.sprite = content.Heals ? healingIcon : IconOf(content.Resource);
            bool showProgress = true;
            if (progressBack != null) progressBack.SetActive(showProgress);
            if (progressFill != null)
            {
                progressFill.gameObject.SetActive(showProgress);
                int height = Mathf.Max(progressHeight, 1);
                progressFill.localScale = new Vector3(1f, Mathf.Round(Mathf.Clamp01(content.Progress) * height) / height, 1f);
            }
        }

        // Справа от строки крафта без квадратика (лист брони) стрелке и полоске не к чему крепиться — прячем.
        bool anchored = showIcon || !content.Beside;
        if (pointer != null) pointer.gameObject.SetActive(anchored);
        if (hpGroup != null) hpGroup.SetActive(content.HasHp && anchored);
        if (content.HasHp && hpFill != null)
        {
            int width = Mathf.Max(hpWidth, 1);
            hpFill.localScale = new Vector3(Mathf.Round(Mathf.Clamp01((float)content.Hp / content.MaxHp) * width) / width, 1f, 1f);
        }

        bool hasTrait = !string.IsNullOrEmpty(content.TraitName);
        if (side != null) side.gameObject.SetActive(hasTrait);
        if (hasTrait)
        {
            if (traitName != null)
            {
                traitName.text = content.TraitName;
                var material = content.TraitMaterial != null ? content.TraitMaterial : defaultTraitMaterial;
                if (material != null && traitName.fontSharedMaterial != material) traitName.fontSharedMaterial = material;
            }
            if (traitInfo != null) traitInfo.text = content.TraitInfo;
        }

        transform.position = new Vector3(Mathf.Round(anchor.x), Mathf.Round(anchor.y), 0f);
        if (above != null)
        {
            if (content.Beside) LayoutBeside(content.Size);
            else LayoutAbove(anchor, content.Size);
        }
        if (side != null) side.localPosition = new Vector3(Mathf.Round(content.Size.x / 2f), 0f, 0f);
    }

    // Над станком (или под ним, если сверху не влезает): квадратик, хп, стрелка вниз к станку.
    private void LayoutAbove(Vector3 anchor, Vector2 size)
    {
        float offset = Mathf.Round(size.y / 2f + gap);
        bool below = DoesNotFitAbove(anchor.y + offset + aboveHeight);
        above.localPosition = new Vector3(0f, below ? -offset : offset, 0f);
        above.localScale = new Vector3(1f, below ? -1f : 1f, 1f);
        if (resourceIcon != null) resourceIcon.flipY = below;
        if (pointer != null)
        {
            pointer.localPosition = pointerPosition;
            pointer.localRotation = pointerRotation;
        }
        if (resourceGroup != null) resourceGroup.transform.localPosition = resourcePosition;
        if (hpGroup != null) hpGroup.transform.localPosition = hpPosition;
    }

    // Справа от станка: квадратик по центру станка, хп под ним, стрелка влево к станку.
    private void LayoutBeside(Vector2 size)
    {
        above.localPosition = new Vector3(Mathf.Round(size.x / 2f + gap), 0f, 0f);
        above.localScale = Vector3.one;
        if (resourceIcon != null) resourceIcon.flipY = false;
        if (pointer != null)
        {
            pointer.localPosition = besidePointer;
            pointer.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }
        if (resourceGroup != null) resourceGroup.transform.localPosition = besideResource;
        if (hpGroup != null) hpGroup.transform.localPosition = besideHp;
    }

    private static bool DoesNotFitAbove(float top)
    {
        var cam = Camera.main;
        return cam != null && top > cam.transform.position.y + cam.orthographicSize;
    }

    public void Hide()
    {
        visible = false;
        if (progress <= 0f) root.SetActive(false);
    }

    private Sprite IconOf(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood: return woodIcon;
            case ResourceType.Scrap: return scrapIcon;
            default: return ingotIcon;
        }
    }

    private void LateUpdate()
    {
        if (!root.activeSelf) return;

        float duration = visible ? showTime : hideTime;
        float step = duration > 0f ? Time.unscaledDeltaTime / duration : 1f;
        progress = Mathf.Clamp01(progress + (visible ? step : -step));
        Animate();

        if (!visible && progress <= 0f) root.SetActive(false);
    }

    private void Animate()
    {
        float scale;
        float alpha;
        float offset;
        if (visible)
        {
            float back = progress - 1f;
            scale = Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
            alpha = Mathf.Clamp01(progress * 4f);
            offset = Mathf.Round(rise * back * back * back);
        }
        else
        {
            scale = Mathf.Lerp(0.85f, 1f, progress);
            alpha = progress;
            offset = 0f;
        }

        if (pop != null)
        {
            pop.localScale = new Vector3(scale, scale, 1f);
            pop.localPosition = new Vector3(0f, offset, 0f);
        }
        for (int i = 0; i < sprites.Length; i++)
        {
            var color = UiPalette.For(sprites[i], spriteColors[i]);
            color.a *= alpha;
            sprites[i].color = color;
        }
        foreach (var text in texts) text.alpha = alpha;
    }
}
