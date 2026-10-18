using System;

// Связывает WagonModel и WagonView: модель изменилась -> обновить картинку.
public class WagonPresenter : IDisposable
{
    private readonly WagonModel model;
    private readonly WagonView view;

    public WagonPresenter(WagonModel model, WagonView view)
    {
        this.model = model;
        this.view = view;

        view.SetSprite(model.Config.sprite);
        UpdateHp(model.Hp);

        model.HpChanged += UpdateHp;
        model.Destroyed += OnDestroyed;
    }

    private void UpdateHp(int hp) => view.SetHp((float)hp / model.Config.maxHp);

    private void OnDestroyed() => view.ShowDestroyed();

    // Отписаться от модели, когда вагон больше не нужен.
    public void Dispose()
    {
        model.HpChanged -= UpdateHp;
        model.Destroyed -= OnDestroyed;
    }
}
