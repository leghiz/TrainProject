using UnityEngine;
using UnityEngine.EventSystems;

public static class PointerOverUI
{
    // Открыто окно поверх игры (экран станции): всё, что под ним, не наводится и не нажимается.
    public static Transform Modal { get; set; }

    // Мышь над меню, открытым прямо в мире (меню плавильни): то, что под ним, не наводится и не нажимается.
    public static Transform Panel { get; set; }

    public static bool Check() => Modal != null || Panel != null || OverCanvas();

    // Для того, что само лежит в открытом окне или меню: ему они не мешают.
    public static bool Check(Component asker) => Blocks(Modal, asker) || Blocks(Panel, asker) || OverCanvas();

    private static bool Blocks(Transform window, Component asker) =>
        window != null && (asker == null || !asker.transform.IsChildOf(window));

    public static bool OverCanvas() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
