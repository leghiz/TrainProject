using UnityEngine;

// Экран карты. Геймдиз растягивает NodesArea и оформляет образцы, код только расставляет копии.
public class MapScreenView : MonoBehaviour
{
    [Tooltip("Прямоугольник, внутри которого раскладываются узлы: слева Старт, справа Конечная")]
    [SerializeField] private RectTransform nodesArea;

    [Header("Образцы (выключены, код делает из них копии)")]
    [SerializeField] private MapNodeView nodeTemplate;
    [Tooltip("Линия между узлами: её толщина — высота образца")]
    [SerializeField] private RectTransform lineTemplate;
    [SerializeField] private MapEventView eventTemplate;

    public void Show() => gameObject.SetActive(true);

    public void Hide() => gameObject.SetActive(false);

    // position — доля от NodesArea: (0, 0) левый нижний угол, (1, 1) правый верхний.
    public MapNodeView AddNode(Vector2 position)
    {
        var node = Instantiate(nodeTemplate, nodesArea);
        node.gameObject.SetActive(true);
        Place((RectTransform)node.transform, position);
        return node;
    }

    public MapEventView AddEvent(Vector2 position)
    {
        var view = Instantiate(eventTemplate, nodesArea);
        view.gameObject.SetActive(true);
        Place((RectTransform)view.transform, position);
        return view;
    }

    public void AddLine(Vector2 from, Vector2 to)
    {
        var line = Instantiate(lineTemplate, nodesArea);
        line.gameObject.SetActive(true);
        line.SetAsFirstSibling(); // линии под узлами
        Vector2 a = ToLocal(from), b = ToLocal(to), delta = b - a;
        line.anchorMin = line.anchorMax = Vector2.zero;
        line.pivot = new Vector2(0f, 0.5f);
        line.anchoredPosition = a;
        line.sizeDelta = new Vector2(delta.magnitude, line.sizeDelta.y);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private void Place(RectTransform target, Vector2 position)
    {
        target.anchorMin = target.anchorMax = Vector2.zero;
        target.anchoredPosition = ToLocal(position);
    }

    private Vector2 ToLocal(Vector2 position) => Vector2.Scale(position, nodesArea.rect.size);
}
