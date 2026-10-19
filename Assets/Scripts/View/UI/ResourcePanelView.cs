using TMPro;
using UnityEngine;

// Панель ресурсов: числа и надписи/иконки геймдиз расставляет как хочет.
public class ResourcePanelView : MonoBehaviour
{
    [Tooltip("Куда писать количество. Любое поле можно оставить пустым")]
    [SerializeField] private TMP_Text woodAmount;
    [SerializeField] private TMP_Text scrapAmount;
    [SerializeField] private TMP_Text ingotAmount;

    public void Set(int wood, int scrap, int ingots)
    {
        if (woodAmount != null) woodAmount.text = wood.ToString();
        if (scrapAmount != null) scrapAmount.text = scrap.ToString();
        if (ingotAmount != null) ingotAmount.text = ingots.ToString();
    }
}
