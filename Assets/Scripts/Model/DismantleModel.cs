using System;

// Разборка удержанием ПКМ: пока держат — копится прогресс, дошёл до конца — выполняется разборка.
public class DismantleModel
{
    public float Duration { get; }
    public float Progress { get; private set; }
    public bool IsActive => complete != null;

    public event Action Changed;

    private object owner;
    private Action complete;

    public DismantleModel(float duration)
    {
        Duration = duration;
    }

    public void Begin(object who, Action onComplete)
    {
        owner = who;
        complete = onComplete;
        Progress = 0f;
        Changed?.Invoke();
    }

    // Отменить может только тот, кто начал (мышь ушла с его предмета).
    public void Cancel(object who)
    {
        if (owner != who) return;
        CancelAny();
    }

    public void CancelAny()
    {
        if (!IsActive) return;
        owner = null;
        complete = null;
        Progress = 0f;
        Changed?.Invoke();
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;
        Progress = Duration > 0f ? Math.Min(1f, Progress + deltaTime / Duration) : 1f;
        Changed?.Invoke();
        if (Progress < 1f) return;

        var action = complete;
        owner = null;
        complete = null;
        Progress = 0f;
        Changed?.Invoke();
        action();
    }
}
