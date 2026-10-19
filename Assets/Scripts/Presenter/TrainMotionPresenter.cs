public class TrainMotionPresenter
{
    private readonly JourneyModel journey;
    private readonly TrainModel train;
    private readonly TrainMotionView view;

    public TrainMotionPresenter(JourneyModel journey, TrainModel train, TrainMotionView view)
    {
        this.journey = journey;
        this.train = train;
        this.view = view;
    }

    public void Tick()
    {
        if (view == null) return;
        view.SetTargetSpeed(journey.State == JourneyState.Travelling ? train.Speed : 0f);
    }
}
