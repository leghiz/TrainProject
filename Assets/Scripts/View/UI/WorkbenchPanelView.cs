using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkbenchPanelView : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    public event Action CloseClicked;

    public bool IsOpen => gameObject.activeSelf;

    // Все строки, которые геймдиз положил внутрь панели (в том числе выключенные).
    public IReadOnlyList<RecipeRowView> Rows => GetComponentsInChildren<RecipeRowView>(true);

    private void Awake() => closeButton.onClick.AddListener(() => CloseClicked?.Invoke());

    public void Show() => gameObject.SetActive(true);

    public void Hide() => gameObject.SetActive(false);
}
