using UnityEngine;
using UnityEngine.UI;

// Значок погоды на дорожке карты (Дождь, Туман, Ветер). Образец лежит в Canvas выключенным.
public class MapEventView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text label;

    public void Setup(Sprite sprite, string name)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        if (label != null) label.text = name;
    }
}
