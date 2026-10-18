using UnityEngine;
public class StormView : MonoBehaviour
{
    public void SetX(float x)
    {
        var position = transform.localPosition;
        position.x = x;
        transform.localPosition = position;
    }
}
