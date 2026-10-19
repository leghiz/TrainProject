using UnityEngine;
using UnityEngine.UI;

// Панель ресурсов лежит в Canvas: числа и надписи/иконки геймдиз расставляет как хочет.
public class ResourcePanelView : MonoBehaviour
{
    [Tooltip("Куда писать количество. Любое поле можно оставить пустым")]
    [SerializeField] private Text woodAmount;
    [SerializeField] private Text scrapAmount;
    [SerializeField] private Text ingotAmount;

    public void Set(int wood, int scrap, int ingots)
    {
        if (woodAmount != null) woodAmount.text = wood.ToString();
        if (scrapAmount != null) scrapAmount.text = scrap.ToString();
        if (ingotAmount != null) ingotAmount.text = ingots.ToString();
    }
}
