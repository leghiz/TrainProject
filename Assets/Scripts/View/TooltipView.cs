using UnityEngine;

public class TooltipView : MonoBehaviour
{
    [Tooltip("Всё содержимое тултипа: его прячем и показываем")]
    [SerializeField] private GameObject root;

    [Tooltip("Фон: по его размеру тултип не даёт себе уехать за край экрана")]
    [SerializeField] private SpriteRenderer background;

    [SerializeField] private TextMesh title;
    [SerializeField] private TextMesh info;

    [Header("Хп (можно не заполнять)")]
    [Tooltip("Прячется, если у объекта нет хп")]
    [SerializeField] private GameObject hpGroup;
    [Tooltip("Точка у левого края полоски: её масштаб по X = доля хп")]
    [SerializeField] private Transform hpFill;
    [SerializeField] private TextMesh hpText;

    [Header("Положение")]
    [Tooltip("Выключено — тултип стоит там, где его поставили в сцене. Включено — появляется над объектом под мышкой")]
    [SerializeField] private bool followTarget = true;

    [Header("Вагон (можно не заполнять)")]
    [Tooltip("Отдельный тултип для вагона: если задан, вагон показывается в нём вместо общего")]
    [SerializeField] private WagonTooltipView wagonTooltip;

    [Header("Станок (можно не заполнять)")]
    [Tooltip("Отдельный тултип для станка: если задан, станок показывается в нём вместо общего")]
    [SerializeField] private MachineTooltipView machineTooltip;

    [Header("Ящик посылки (можно не заполнять)")]
    [Tooltip("Отдельный тултип для ящика: если задан, ящик показывается в нём вместо общего")]
    [SerializeField] private BoxTooltipView boxTooltip;

    public void Show(TooltipContent content, Vector3 anchor)
    {
        if (content.IsBox && boxTooltip != null)
        {
            root.SetActive(false);
            if (wagonTooltip != null) wagonTooltip.Hide();
            if (machineTooltip != null) machineTooltip.Hide();
            boxTooltip.Show(content, anchor);
            return;
        }
        if (boxTooltip != null) boxTooltip.Hide();

        if (content.IsWagon && wagonTooltip != null)
        {
            root.SetActive(false);
            if (machineTooltip != null) machineTooltip.Hide();
            wagonTooltip.Show(content, anchor);
            return;
        }
        if (content.IsMachine && machineTooltip != null)
        {
            root.SetActive(false);
            if (wagonTooltip != null) wagonTooltip.Hide();
            machineTooltip.Show(content, anchor);
            return;
        }
        if (wagonTooltip != null) wagonTooltip.Hide();
        if (machineTooltip != null) machineTooltip.Hide();

        root.SetActive(true);
        if (title != null) title.text = content.Title;
        if (info != null) info.text = content.Info;
        if (hpGroup != null) hpGroup.SetActive(content.HasHp);
        if (content.HasHp)
        {
            if (hpFill != null) hpFill.localScale = new Vector3(Mathf.Clamp01((float)content.Hp / content.MaxHp), 1f, 1f);
            if (hpText != null) hpText.text = $"{content.Hp}/{content.MaxHp}";
        }

        if (!followTarget) return;
        transform.position = new Vector3(Mathf.Round(anchor.x), Mathf.Round(anchor.y), 0f);
        KeepOnScreen();
    }

    public void Hide()
    {
        root.SetActive(false);
        if (wagonTooltip != null) wagonTooltip.Hide();
        if (machineTooltip != null) machineTooltip.Hide();
        if (boxTooltip != null) boxTooltip.Hide();
    }
    
    private void KeepOnScreen()
    {
        var cam = Camera.main;
        if (cam == null || background == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 center = cam.transform.position;
        var bounds = background.bounds;

        float dx = 0f, dy = 0f;
        if (bounds.min.x < center.x - halfWidth) dx = center.x - halfWidth - bounds.min.x;
        else if (bounds.max.x > center.x + halfWidth) dx = center.x + halfWidth - bounds.max.x;
        if (bounds.max.y > center.y + halfHeight) dy = center.y + halfHeight - bounds.max.y;
        else if (bounds.min.y < center.y - halfHeight) dy = center.y - halfHeight - bounds.min.y;

        transform.position += new Vector3(Mathf.Round(dx), Mathf.Round(dy), 0f);
    }
}
