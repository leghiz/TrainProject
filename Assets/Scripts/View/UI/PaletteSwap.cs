using TMPro;
using UnityEngine;

// Вешается на корень интерфейса: когда палитра переворачивается (экран выбора вагонов),
// чёрные плашки внутри становятся белыми, а белые цифры — чёрными. Прозрачность не трогает.
public class PaletteSwap : MonoBehaviour
{
    [Tooltip("Что не перекрашивать (например, затемнение экрана)")]
    [SerializeField] private Renderer[] keep;

    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private TMP_Text[] texts;
    private Color[] textColors;

    private void Awake()
    {
        var all = GetComponentsInChildren<SpriteRenderer>(true);
        var list = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sprite in all)
            if (keep == null || System.Array.IndexOf(keep, sprite) < 0) list.Add(sprite);
        sprites = list.ToArray();
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;

        texts = GetComponentsInChildren<TMP_Text>(true);
        textColors = new Color[texts.Length];
        for (int i = 0; i < texts.Length; i++) textColors[i] = texts[i].color;
    }

    private void OnEnable()
    {
        UiPalette.Changed += Apply;
        Apply();
    }

    private void OnDisable() => UiPalette.Changed -= Apply;

    private void Apply()
    {
        if (sprites == null) return;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            var color = UiPalette.For(sprites[i], spriteColors[i]);
            color.a = sprites[i].color.a;
            sprites[i].color = color;
        }
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;
            var color = UiPalette.Text(textColors[i]);
            color.a = texts[i].color.a;
            texts[i].color = color;
        }
    }
}
