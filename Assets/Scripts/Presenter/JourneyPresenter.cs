using System;

public class JourneyPresenter : IDisposable
{
    private readonly JourneyModel model;
    private readonly TrainView trainView;

    public JourneyPresenter(JourneyModel model, TrainView trainView)
    {
        this.model = model;
        this.trainView = trainView;

        UpdateFog(model.State);
        model.StateChanged += UpdateFog;
    }

    private void UpdateFog(JourneyState state) =>
        trainView.SetFog(state == JourneyState.Travelling && model.HasEvent(TravelEventType.Fog));

    public void Dispose()
    {
        model.StateChanged -= UpdateFog;
    }
}
