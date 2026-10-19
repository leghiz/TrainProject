using UnityEngine;

public class HoverOutlineBlocker : MonoBehaviour
{
    private void OnEnable() => HoverOutline.Suppressed = true;

    private void OnDisable() => HoverOutline.Suppressed = false;
}
