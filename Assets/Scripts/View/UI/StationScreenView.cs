using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Экран станции. Всё оформление — в Canvas. Карточки вагонов и посылок делаются из образцов
// и раскладываются в ряд внутри своих областей.
public class StationScreenView : MonoBehaviour
{
    [SerializeField] private Text title;
    [Tooltip("«Можно взять: 1» (можно не заполнять)")]
    [SerializeField] private Text picksLeft;

    [Header("Вагоны")]
    [Tooltip("Прямоугольник, внутри которого карточки вагонов встают в ряд")]
    [SerializeField] private RectTransform cardsArea;
    [Tooltip("Образец карточки вагона (выключен, код делает из него копии)")]
    [SerializeField] private WagonCardView cardTemplate;

    [Header("Посылки (только на Грузовой)")]
    [Tooltip("Блок посылок целиком: прячется, если посылок нет")]
    [SerializeField] private GameObject cargoBlock;
    [Tooltip("Прямоугольник, внутри которого карточки посылок встают в ряд")]
    [SerializeField] private RectTransform cargoCardsArea;
    [Tooltip("Образец карточки посылки (выключен)")]
    [SerializeField] private WagonCardView cargoCardTemplate;

    [Header("Уехать")]
    [SerializeField] private Button leaveButton;

    public event Action LeaveClicked;

    private readonly List<WagonCardView> cards = new List<WagonCardView>();

    private void Awake() => leaveButton.onClick.AddListener(() => LeaveClicked?.Invoke());

    public void Show() => gameObject.SetActive(true);

    public void Hide() => gameObject.SetActive(false);

    public void SetTitle(string text) => title.text = text;

    public void SetPicksLeft(string text)
    {
        if (picksLeft != null) picksLeft.text = text;
    }

    public void ClearCards()
    {
        foreach (var card in cards) Destroy(card.gameObject);
        cards.Clear();
    }

    public WagonCardView AddCard(int index, int count) => Add(cardTemplate, cardsArea, index, count);

    public WagonCardView AddCargoCard(int index, int count) => Add(cargoCardTemplate, cargoCardsArea, index, count);

    public void SetCargoVisible(bool visible) => cargoBlock.SetActive(visible);

    // index из count — карточки ровно делят ширину области.
    private WagonCardView Add(WagonCardView template, RectTransform area, int index, int count)
    {
        var card = Instantiate(template, area);
        card.gameObject.SetActive(true);
        var rt = (RectTransform)card.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = new Vector2(area.rect.width * (index + 1f) / (count + 1f), area.rect.height / 2f);
        cards.Add(card);
        return card;
    }
}
