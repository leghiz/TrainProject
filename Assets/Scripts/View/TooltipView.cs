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

    public void Show(TooltipContent content, Vector3 anchor)
    {
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

    public void Hide() => root.SetActive(false);
    
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
