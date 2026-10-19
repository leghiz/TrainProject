using System;
using UnityEngine;
using UnityEngine.UI;

// Карточка вагона на станции. Образец лежит в Canvas выключенным — код делает из него копии.
public class WagonCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image picture;
    [SerializeField] private Text title;
    [SerializeField] private Text info;

    public event Action Clicked;

    private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());

    public void Setup(Sprite sprite, string name, string details)
    {
        if (picture != null)
        {
            picture.sprite = sprite;
            picture.enabled = sprite != null;
        }
        if (title != null) title.text = name;
        if (info != null) info.text = details;
    }

    public void SetInteractable(bool value) => button.interactable = value;
}
