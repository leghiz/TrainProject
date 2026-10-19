using System;

public class MachinePresenter : IDisposable
{
    private readonly MachineModel model;
    private readonly MachineView view;

    public MachinePresenter(MachineModel model, MachineView view)
    {
        this.model = model;
        this.view = view;

        view.SetSprite(model.Config.sprite);
        if (model.Trait != null) view.SetMaterial(model.Trait.material);
        view.SetWorking(model.IsWorking);

        model.WorkingChanged += view.SetWorking;
    }

    public void Dispose()
    {
        model.WorkingChanged -= view.SetWorking;
    }
}
