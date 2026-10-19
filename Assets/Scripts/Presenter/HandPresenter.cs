using System;

public class HandPresenter : IDisposable
{
    private readonly BuildModel build;
    private readonly HandView view;

    public HandPresenter(BuildModel build, HandView view)
    {
        this.build = build;
        this.view = view;

        OnSelectedChanged(build.Selected);
        build.SelectedChanged += OnSelectedChanged;
        view.Released += build.Cancel;
        view.CancelPressed += build.Cancel;
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        if (machine != null) view.Show(machine.sprite);
        else view.Hide();
    }

    public void Dispose()
    {
        build.SelectedChanged -= OnSelectedChanged;
        view.Released -= build.Cancel;
        view.CancelPressed -= build.Cancel;
    }
}
