using UnityEngine;

// Полоска пути вверху экрана, по центру. Красное — буря, зелёное — мы, жёлтое — сколько осталось до станции,
// её иконка справа. Под краем красного едет стрелка со значком бури, под краем зелёного — наша стрелка.
public class JourneyProgressView : MonoBehaviour
{
    [Tooltip("Всё содержимое полоски: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Ширина полоски внутри рамки, пиксели: заливки и стрелки встают по целым пикселям")]
    [SerializeField] private int width = 78;
    [Tooltip("Точка у левого края зелёной заливки: её масштаб по X = где мы")]
    [SerializeField] private Transform usFill;
    [Tooltip("Точка у левого края красной заливки: её масштаб по X = где буря")]
    [SerializeField] private Transform stormFill;
    [Tooltip("Наша стрелка под полоской: едет за краем зелёного")]
    [SerializeField] private Transform usMarker;
    [Tooltip("Стрелка бури со значком: едет за краем красного")]
    [SerializeField] private Transform stormMarker;
    [Tooltip("Иконка станции, куда едем")]
    [SerializeField] private SpriteRenderer destinationIcon;

    [Header("Где мы и где буря")]
    [Tooltip("Насколько мы впереди в самом начале перегона, доля полоски: буря всегда стартует чуть позади")]
    [Range(0f, 0.5f)] [SerializeField] private float lead = 0.1f;
    [Tooltip("Сколько клеток от бури до хвоста поезда умещается во всю полоску: чем больше, тем ближе к нам кажется буря")]
    [Min(1f)] [SerializeField] private float cellsPerBar = 40f;

    public void Show() => root.SetActive(true);

    public void Hide() => root.SetActive(false);

    public void SetDestination(Sprite icon)
    {
        if (destinationIcon == null) return;
        destinationIcon.sprite = icon;
        destinationIcon.enabled = icon != null;
    }

    // progress — доля пройденного перегона (0..1); stormDistance — сколько клеток от бури до хвоста поезда.
    public void SetProgress(float progress, float stormDistance)
    {
        float us = Mathf.Lerp(lead, 1f, Mathf.Clamp01(progress));
        float storm = Mathf.Clamp(us - stormDistance / cellsPerBar, 0f, us);
        Place(usFill, usMarker, us);
        Place(stormFill, stormMarker, storm);
    }

    private void Place(Transform fill, Transform marker, float share)
    {
        int full = Mathf.Max(width, 1);
        int pixels = Mathf.RoundToInt(share * full);
        if (fill != null) fill.localScale = new Vector3((float)pixels / full, 1f, 1f);
        if (marker == null) return;
        var position = marker.localPosition;
        position.x = pixels - full / 2f;
        marker.localPosition = position;
    }
}
