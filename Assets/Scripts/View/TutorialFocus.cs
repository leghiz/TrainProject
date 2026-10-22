using UnityEngine;

// Обучение: пока оно идёт, в мире нажимается только цель (и то, что внутри неё). Остальное на клики не реагирует.
public static class TutorialFocus
{
    public static bool Active { get; set; }
    public static Transform Target { get; set; }

    public static bool Allows(Transform clicked) =>
        !Active || (Target != null && clicked != null && (clicked == Target || clicked.IsChildOf(Target)));
}
