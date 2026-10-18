using UnityEngine;
using UnityEngine.InputSystem;

// ВРЕМЕННЫЙ тест: Config -> Model -> Presenter -> View.
// D — урон, R — ремонт, пробел — другой случайный вагон.
public class WagonSandbox : MonoBehaviour
{
    [SerializeField] private WagonConfig[] wagons;
    [SerializeField] private WagonView wagonPrefab;

    private WagonModel model;
    private WagonView view;
    private WagonPresenter presenter;

    private void Start() => Spawn();

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.dKey.wasPressedThisFrame) model.Damage(10);
        if (kb.rKey.wasPressedThisFrame) model.Repair(10);
        if (kb.spaceKey.wasPressedThisFrame) Spawn();
    }

    private void Spawn()
    {
        presenter?.Dispose();
        if (view != null) Destroy(view.gameObject);

        var config = wagons[Random.Range(0, wagons.Length)];
        model = new WagonModel(config);
        view = Instantiate(wagonPrefab);
        presenter = new WagonPresenter(model, view);
        Debug.Log($"Вагон: {config.displayName}, хп {model.Hp}");
    }
}
