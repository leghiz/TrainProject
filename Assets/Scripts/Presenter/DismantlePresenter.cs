using System;

// Разборка удержанием ПКМ: отпустили раньше времени — отмена.
// Кружок прогресса делает напарник: ему нужны DismantleModel.IsActive, Progress (0..1) и событие Changed.
public class DismantlePresenter : IDisposable
{
    private readonly DismantleModel model;
    private readonly HandView hand;

    public DismantlePresenter(DismantleModel model, HandView hand)
    {
        this.model = model;
        this.hand = hand;

        hand.RightReleased += model.CancelAny;
    }

    public void Dispose() => hand.RightReleased -= model.CancelAny;
}
