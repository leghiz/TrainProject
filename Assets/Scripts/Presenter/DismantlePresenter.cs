using System;

// Удержание ПКМ (разборка) и ЛКМ (взять станок в руку): отпустили раньше времени — отмена.
// Кружок прогресса — HoldProgressView: он читает DismantleModel.IsActive и Progress (0..1).
public class DismantlePresenter : IDisposable
{
    private readonly DismantleModel model;
    private readonly HandView hand;

    public DismantlePresenter(DismantleModel model, HandView hand)
    {
        this.model = model;
        this.hand = hand;

        hand.RightReleased += model.CancelAny;
        hand.Released += model.CancelAny;
    }

    public void Dispose()
    {
        hand.RightReleased -= model.CancelAny;
        hand.Released -= model.CancelAny;
    }
}
