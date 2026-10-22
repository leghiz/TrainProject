using TMPro;
using UnityEngine;

// Панель ресурсов: числа и надписи/иконки геймдиз расставляет как хочет.
// Прибавилось ресурса — его число вместе с рамкой подпрыгивает.
public class ResourcePanelView : MonoBehaviour
{
    [Tooltip("Куда писать количество. Любое поле можно оставить пустым")]
    [SerializeField] private TMP_Text woodAmount;
    [SerializeField] private TMP_Text scrapAmount;
    [SerializeField] private TMP_Text ingotAmount;

    [Header("Прибавилось — число подпрыгивает")]
    [Tooltip("Рамки под числами: подпрыгивают вместе с числом. Можно оставить пустыми")]
    [SerializeField] private Transform woodBox;
    [SerializeField] private Transform scrapBox;
    [SerializeField] private Transform ingotBox;
    [Tooltip("Во сколько раз число раздувается в самом начале")]
    [SerializeField] private float popScale = 1.5f;
    [Tooltip("За сколько секунд число возвращается к своему размеру")]
    [SerializeField] private float popTime = 0.3f;

    // Куда прилетает слиток из награды за доставку: число слитков.
    public Vector3 IngotPoint => ingotAmount != null ? ingotAmount.transform.position : transform.position;

    private readonly int[] last = { -1, -1, -1 };
    private readonly float[] popTimers = { -1f, -1f, -1f };
    private Transform[][] targets;
    private Vector3[][] baseScales;

    private void Awake()
    {
        targets = new[]
        {
            Targets(woodAmount, woodBox),
            Targets(scrapAmount, scrapBox),
            Targets(ingotAmount, ingotBox)
        };
        baseScales = new Vector3[targets.Length][];
        for (int i = 0; i < targets.Length; i++)
        {
            baseScales[i] = new Vector3[targets[i].Length];
            for (int j = 0; j < targets[i].Length; j++) baseScales[i][j] = targets[i][j].localScale;
        }
    }

    private static Transform[] Targets(TMP_Text amount, Transform box)
    {
        if (amount == null) return box != null ? new[] { box } : new Transform[0];
        return box != null ? new[] { amount.transform, box } : new[] { amount.transform };
    }

    public void Set(int wood, int scrap, int ingots)
    {
        Apply(0, woodAmount, wood);
        Apply(1, scrapAmount, scrap);
        Apply(2, ingotAmount, ingots);
    }

    private void Apply(int index, TMP_Text amount, int value)
    {
        if (amount != null) amount.text = value.ToString();
        if (last[index] >= 0 && value > last[index]) popTimers[index] = 0f;
        last[index] = value;
    }

    private void LateUpdate()
    {
        if (targets == null) return;
        for (int i = 0; i < popTimers.Length; i++)
        {
            if (popTimers[i] < 0f) continue;
            popTimers[i] += Time.unscaledDeltaTime;
            float t = popTime > 0f ? Mathf.Clamp01(popTimers[i] / popTime) : 1f;
            float back = 1f - t;
            float scale = 1f + (popScale - 1f) * back * back * Mathf.Cos(t * Mathf.PI * 1.5f);
            for (int j = 0; j < targets[i].Length; j++) targets[i][j].localScale = baseScales[i][j] * scale;
            if (t >= 1f) popTimers[i] = -1f;
        }
    }
}
