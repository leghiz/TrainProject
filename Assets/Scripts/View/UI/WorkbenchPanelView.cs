using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Меню верстака висит в мире над верстаком: стрелкой вниз на него. Закрывается ПКМ или Esc.
public class WorkbenchPanelView : MonoBehaviour
{
    [Tooltip("Всё содержимое меню: его прячем и показываем")]
    [SerializeField] private GameObject root;
    [Tooltip("Точка у кончика нижней стрелки: вокруг неё меню раздувается при появлении")]
    [SerializeField] private Transform pop;
    [Tooltip("Над чем висит меню: верстак на локомотиве (WorkbenchSlot)")]
    [SerializeField] private Transform anchor;
    [Tooltip("Где кончик стрелки относительно центра верстака, пиксели")]
    [SerializeField] private Vector2 anchorOffset = new Vector2(0f, 10f);

    [Header("Появление")]
    [SerializeField] private float showTime = 0.18f;
    [SerializeField] private float startScale = 0.4f;
    [Tooltip("Пружинка: насколько размер перелетает единицу")]
    [SerializeField] private float overshoot = 1.8f;

    public event Action CloseClicked;

    public bool IsOpen => root.activeSelf;

    // Все строки внутри меню (в том числе выключенные).
    public IReadOnlyList<RecipeRowView> Rows => GetComponentsInChildren<RecipeRowView>(true);

    private float time;

    public void Show()
    {
        root.SetActive(true);
        time = 0f;
        Place();
        Animate();
    }

    public void Hide() => root.SetActive(false);

    private void LateUpdate()
    {
        if (!root.activeSelf) return;

        time += Time.unscaledDeltaTime;
        Place();
        Animate();

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            CloseClicked?.Invoke();
    }

    private void Place()
    {
        if (anchor == null) return;
        var target = anchor.position + (Vector3)anchorOffset;
        transform.position = new Vector3(Mathf.Round(target.x), Mathf.Round(target.y), transform.position.z);
    }

    private void Animate()
    {
        if (pop == null) return;
        float t = showTime > 0f ? Mathf.Clamp01(time / showTime) : 1f;
        float back = t - 1f;
        float scale = Mathf.LerpUnclamped(startScale, 1f, 1f + (overshoot + 1f) * back * back * back + overshoot * back * back);
        pop.localScale = new Vector3(scale, scale, 1f);
    }
}
