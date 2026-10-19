using UnityEngine;
using UnityEngine.EventSystems;

public static class PointerOverUI
{
    // Открыто окно поверх игры (экран станции): всё, что под ним, не наводится и не нажимается.
    public static Transform Modal { get; set; }

    public static bool Check() => Modal != null || OverCanvas();

    // Для того, что само лежит в открытом окне: ему окно не мешает.
    public static bool Check(Component asker) =>
        (Modal != null && (asker == null || !asker.transform.IsChildOf(Modal))) || OverCanvas();

    public static bool OverCanvas() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
