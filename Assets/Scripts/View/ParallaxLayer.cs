using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("Доля скорости поезда: 0 — не едет (небо), 1 — земля у рельсов, больше 1 — передний план")]
    [SerializeField] private float speed = 1f;
    [Tooltip("Насколько слой следует за камерой: 1 — висит вместе с камерой (небо, сплошная земля), 0 — стоит в мире")]
    [Range(0f, 1f)] [SerializeField] private float followCamera;
    [Tooltip("Через сколько пикселей детали слоя повторяются по кругу, лучше целое число; 0 — не зацикливать")]
    [SerializeField] private float loopWidth = 480f;

    private Transform[] items;
    private float[] baseX;
    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
        items = new Transform[transform.childCount];
        baseX = new float[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = transform.GetChild(i);
            baseX[i] = items[i].localPosition.x;
        }
    }

    private void LateUpdate()
    {
        var cam = Camera.main;
        float camX = cam != null ? cam.transform.position.x : 0f;
        var motion = TrainMotionView.Current;
        float travelled = motion != null ? Mathf.Round(motion.Distance * speed) : 0f;

        transform.position = new Vector3(startPosition.x + Mathf.Round(camX * followCamera), startPosition.y, startPosition.z);

        for (int i = 0; i < items.Length; i++)
        {
            float x = baseX[i] - travelled;
            if (loopWidth > 0f)
            {
                float relative = transform.position.x + x - camX;
                float wrapped = Mathf.Repeat(relative + loopWidth * 0.5f, loopWidth) - loopWidth * 0.5f;
                x += Mathf.Round(wrapped - relative);
            }
            var position = items[i].localPosition;
            position.x = x;
            items[i].localPosition = position;
        }
    }
}
