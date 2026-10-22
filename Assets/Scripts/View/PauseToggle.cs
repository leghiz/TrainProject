using UnityEngine;
using UnityEngine.InputSystem;

// Пробел — пауза: время игры стоит, звук молчит, никаких меню. Пробел ещё раз — дальше.
public class PauseToggle : MonoBehaviour
{
    public static bool Paused { get; private set; }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) Set(!Paused);
    }

    private void OnDestroy()
    {
        if (Paused) Set(false);
    }

    private static void Set(bool value)
    {
        Paused = value;
        Time.timeScale = value ? 0f : 1f;
        AudioListener.pause = value;
    }
}
