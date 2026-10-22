using System;

// Полоска пути: пока едем — сколько проехали по времени перегона и как близко буря.
// Видна с отъезда и до открытия станции (экран станции сам прячет её на время).
public class JourneyProgressPresenter : IDisposable
{
    private readonly JourneyModel journey;
    private readonly JourneyProgressView view;

    public JourneyProgressPresenter(JourneyModel journey, JourneyProgressView view)
    {
        this.journey = journey;
        this.view = view;

        journey.StateChanged += OnStateChanged;
        OnStateChanged(journey.State);
    }

    private void OnStateChanged(JourneyState state)
    {
        switch (state)
        {
            case JourneyState.Travelling:
                var station = journey.Target != null ? journey.Target.Station : null;
                view.SetDestination(station != null ? station.icon : null);
                view.Show();
                Tick();
                break;
            case JourneyState.Station:
                view.SetProgress(1f, StormDistance());
                break;
            default:
                view.Hide();
                break;
        }
    }

    public void Tick()
    {
        if (journey.State != JourneyState.Travelling) return;
        float total = journey.Config.travelSeconds;
        view.SetProgress(total > 0f ? 1f - journey.TimeLeft / total : 1f, StormDistance());
    }

    private float StormDistance() => journey.Storm.DistanceToTail(journey.Train);

    public void Dispose() => journey.StateChanged -= OnStateChanged;
}
