using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Экран станции. Всё оформление — в Canvas, карточки вагонов делаются из образца
// и раскладываются в ряд внутри CardsArea.
public class StationScreenView : MonoBehaviour
{
    [SerializeField] private Text title;
    [Tooltip("«Можно взять: 1» (можно не заполнять)")]
    [SerializeField] private Text picksLeft;

    [Header("Вагоны")]
    [Tooltip("Прямоугольник, внутри которого карточки встают в ряд")]
    [SerializeField] private RectTransform cardsArea;
    [Tooltip("Образец карточки (выключен, код делает из него копии)")]
    [SerializeField] private WagonCardView cardTemplate;

    [Header("Посылка (только на Грузовой)")]
    [Tooltip("Блок посылки целиком: прячется, если посылки нет")]
    [SerializeField] private GameObject cargoBlock;
    [SerializeField] private Text cargoText;
    [SerializeField] private Button takeCargoButton;

    [Header("Уехать")]
    [SerializeField] private Button leaveButton;

    public event Action TakeCargoClicked;
    public event Action LeaveClicked;

    private readonly List<WagonCardView> cards = new List<WagonCardView>();

    private void Awake()
    {
        takeCargoButton.onClick.AddListener(() => TakeCargoClicked?.Invoke());
        leaveButton.onClick.AddListener(() => LeaveClicked?.Invoke());
    }

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

    // index из count — карточки ровно делят ширину CardsArea.
    public WagonCardView AddCard(int index, int count)
    {
        var card = Instantiate(cardTemplate, cardsArea);
        card.gameObject.SetActive(true);
        var rt = (RectTransform)card.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = new Vector2(cardsArea.rect.width * (index + 1f) / (count + 1f), cardsArea.rect.height / 2f);
        cards.Add(card);
        return card;
    }

    public void ShowCargo(string text)
    {
        cargoBlock.SetActive(true);
        if (cargoText != null) cargoText.text = text;
    }

    public void HideCargo() => cargoBlock.SetActive(false);
}
