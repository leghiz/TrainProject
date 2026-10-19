using System;

public class WeatherPresenter : IDisposable
{
    private readonly JourneyModel journey;
    private readonly WeatherView view;

    public WeatherPresenter(JourneyModel journey, WeatherView view)
    {
        this.journey = journey;
        this.view = view;

        Refresh(journey.State);
        journey.StateChanged += Refresh;
    }

    private void Refresh(JourneyState state)
    {
        if (view == null) return;
        bool travelling = state == JourneyState.Travelling;
        view.Show(
            travelling && journey.HasEvent(TravelEventType.Rain),
            travelling && journey.HasEvent(TravelEventType.Wind),
            travelling && journey.HasEvent(TravelEventType.Fog));
    }

    public void Dispose() => journey.StateChanged -= Refresh;
}
