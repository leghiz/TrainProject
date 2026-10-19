using UnityEngine;

public class MachineView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;

    [Tooltip("Цвет станка, когда он простаивает (нечего добывать, нет топлива)")]
    [SerializeField] private Color idleColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    public void SetSprite(Sprite sprite) => body.sprite = sprite;

    public void SetMaterial(Material material)
    {
        if (material != null) body.sharedMaterial = material;
    }

    public void SetWorking(bool working) => body.color = working ? Color.white : idleColor;
}
