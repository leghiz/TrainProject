using UnityEngine.EventSystems;

public static class PointerOverUI
{
    public static bool Check() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
