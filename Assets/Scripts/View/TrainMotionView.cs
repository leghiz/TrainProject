using UnityEngine;

public class TrainMotionView : MonoBehaviour
{
    [Tooltip("Сколько пикселей в секунду проезжает земля при скорости поезда 1.0")]
    [SerializeField] private float pixelsPerSpeed = 90f;
    [Tooltip("Разгон и торможение, пикселей в секунду за секунду")]
    [SerializeField] private float acceleration = 120f;

    public static TrainMotionView Current { get; private set; }

    public float Speed { get; private set; }
    public float Distance { get; private set; }
    public float SpeedShare => pixelsPerSpeed > 0f ? Speed / pixelsPerSpeed : 0f;
    public bool IsMoving => Speed > 1f;

    private float targetSpeed;

    private void OnEnable() => Current = this;

    private void OnDisable()
    {
        if (Current == this) Current = null;
    }

    public void SetTargetSpeed(float trainSpeed) => targetSpeed = Mathf.Max(0f, trainSpeed) * pixelsPerSpeed;

    private void Update()
    {
        Speed = Mathf.MoveTowards(Speed, targetSpeed, acceleration * Time.deltaTime);
        Distance += Speed * Time.deltaTime;
    }
}
