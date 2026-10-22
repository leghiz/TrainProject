using System;
using UnityEngine;

// Какой станок сейчас открыт в своём меню. Меню есть только у плавильни: клик по ней — загрузка лома.
public class MachineMenuModel
{
    public MachineModel Machine { get; private set; }
    public Vector3 Anchor { get; private set; }
    public bool IsOpen => Machine != null;

    public event Action Changed;

    public static bool HasMenu(MachineModel machine) => machine != null && machine.Config.action == MachineAction.Convert;

    // Клик по тому же станку закрывает меню, по другому — переключает на него.
    public void Toggle(MachineModel machine, Vector3 anchor)
    {
        if (Machine == machine)
        {
            Close();
            return;
        }
        Machine = machine;
        Anchor = anchor;
        Changed?.Invoke();
    }

    public void Close()
    {
        if (Machine == null) return;
        Machine = null;
        Changed?.Invoke();
    }

    public void Close(MachineModel machine)
    {
        if (Machine == machine) Close();
    }
}
