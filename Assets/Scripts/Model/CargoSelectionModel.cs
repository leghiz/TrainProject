using System;
using UnityEngine;

public class CargoSelectionModel
{
    public CargoBoxModel Box { get; private set; }
    public Vector3 Anchor { get; private set; }

    public event Action Changed;

    public void Select(CargoBoxModel box, Vector3 anchor)
    {
        Box = box;
        Anchor = anchor;
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (Box == null) return;
        Box = null;
        Changed?.Invoke();
    }
}
