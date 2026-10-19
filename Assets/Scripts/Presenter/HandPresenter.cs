using System;
using UnityEngine;

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
        var box = build.MovingBox;
        if (machine != null) view.Show(machine.sprite);
        else if (box != null)
        {
            // Ящик висит под мышкой той клеткой, за которую его взяли.
            Vector2 size = box.Config.size;
            view.ShowArea(box.Config.sprite, size, (size - Vector2.one) / 2f - build.BoxGrab);
        }
        else view.Hide();
    }

    public void Dispose()
    {
        build.SelectedChanged -= OnSelectedChanged;
        view.Released -= build.Cancel;
        view.CancelPressed -= build.Cancel;
    }
}
