using System;
using UnityEngine;
using UnityEngine.UI;

// Строка рецепта лежит прямо в Canvas: её можно двигать, растягивать, дублировать.
// Картинку, название и цену она берёт из выбранного станка — их видно и без Play.
public class RecipeRowView : MonoBehaviour
{
    [Tooltip("Какой станок крафтит эта строка. Он должен быть в списке recipes у Locomotive.asset")]
    [SerializeField, Expandable] private MachineConfig recipe;

    [Header("Куда подставлять данные станка")]
    [SerializeField] private Image icon;
    [SerializeField] private Text title;
    [SerializeField] private Text woodCost;
    [SerializeField] private Text scrapCost;
    [Tooltip("Необязательно: цена в слитках (для листа обшивки)")]
    [SerializeField] private Text ingotCost;
    [SerializeField] private Button craftButton;

    public MachineConfig Recipe => recipe;

    public event Action Clicked;

    private void Awake() => craftButton.onClick.AddListener(() => Clicked?.Invoke());

    public void Refresh()
    {
        if (recipe == null) return;
        if (icon != null)
        {
            icon.sprite = recipe.sprite;
            icon.enabled = recipe.sprite != null;
        }
        if (title != null) title.text = recipe.displayName;
        if (woodCost != null) woodCost.text = recipe.craftWood.ToString();
        if (scrapCost != null) scrapCost.text = recipe.craftScrap.ToString();
        if (ingotCost != null) ingotCost.text = recipe.craftIngots.ToString();
    }

    public void SetInteractable(bool value) => craftButton.interactable = value;

#if UNITY_EDITOR
    // Выбрали другой станок в Inspector — строка сразу показывает его данные.
    // delayCall: менять UI прямо внутри OnValidate Unity не любит.
    private void OnValidate() => UnityEditor.EditorApplication.delayCall += () =>
    {
        if (this != null) Refresh();
    };
#endif
}
