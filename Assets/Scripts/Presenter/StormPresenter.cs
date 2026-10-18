using System;
public class StormPresenter : IDisposable
{
    private readonly StormModel model;
    private readonly StormView view;
    private readonly TrainView trainView;

    public StormPresenter(StormModel model, StormView view, TrainView trainView)
    {
        this.model = model;
        this.view = view;
        this.trainView = trainView;

        UpdatePosition(model.Position);
        model.PositionChanged += UpdatePosition;
    }

    private void UpdatePosition(float cellsBehindLocomotive) =>
        view.SetX(trainView.CellsToX(cellsBehindLocomotive));

    public void Dispose()
    {
        model.PositionChanged -= UpdatePosition;
    }
}
