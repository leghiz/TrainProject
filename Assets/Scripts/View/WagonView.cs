using UnityEngine;

// Отображение вагона в игровом мире. Только показывает то, что ему скажут, логики здесь нет.
public class WagonView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body; // картинка вагона

    [Tooltip("Точка у левого края полоски хп. Её масштаб по X = доля оставшегося хп")]
    [SerializeField] private Transform hpFill;

    public void SetSprite(Sprite sprite) => body.sprite = sprite;

    public void SetHp(float normalized) => hpFill.localScale = new Vector3(normalized, 1f, 1f); // 0..1

    public void ShowDestroyed() => gameObject.SetActive(false); // пока просто прячем
}
