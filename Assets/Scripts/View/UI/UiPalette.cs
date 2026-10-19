using System;
using UnityEngine;

// Палитра интерфейса. На экране выбора вагонов она переворачивается: чёрные плашки и стрелки становятся белыми,
// белые цифры и надписи — чёрными. Цветные иконки и надписи не меняются.
public static class UiPalette
{
    public static bool Inverted { get; private set; }

    public static event Action Changed;

    public static void SetInverted(bool value)
    {
        if (Inverted == value) return;
        Inverted = value;
        Changed?.Invoke();
    }

    public static Color Sprite(Color color) => Inverted && IsBlack(color) ? new Color(1f, 1f, 1f, color.a) : color;

    public static Color Text(Color color) => Inverted && IsWhite(color) ? new Color(0f, 0f, 0f, color.a) : color;

    // Белая иконка на плашке (PaletteIcon): в перевёрнутой палитре чернеет, как цифры.
    public static Color For(SpriteRenderer sprite, Color color) =>
        sprite != null && sprite.GetComponent<PaletteIcon>() != null ? Text(color) : Sprite(color);

    private static bool IsBlack(Color c) => c.r < 0.1f && c.g < 0.1f && c.b < 0.1f;

    private static bool IsWhite(Color c) => c.r > 0.9f && c.g > 0.9f && c.b > 0.9f;
}
