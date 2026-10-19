using UnityEngine;

public class CargoBoxView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    
    public void Setup(Sprite sprite, Vector2 areaSize)
    {
        if (sprite != null)
        {
            body.sprite = sprite;
            body.transform.localScale = Vector3.one;
        }
        else
        {
            body.transform.localScale = new Vector3(areaSize.x, areaSize.y, 1f);
        }
    }
}
