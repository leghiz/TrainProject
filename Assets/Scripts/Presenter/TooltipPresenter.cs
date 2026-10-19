using System;

public class TooltipPresenter : IDisposable
{
    private readonly HoverModel hover;
    private readonly TooltipView view;

    public TooltipPresenter(HoverModel hover, TooltipView view)
    {
        this.hover = hover;
        this.view = view;

        view.Hide();
        hover.Changed += Refresh;
    }

    public void Tick() => Refresh();

    private void Refresh()
    {
        if (!hover.HasTarget || PointerOverUI.Check())
        {
            view.Hide();
            return;
        }
        view.Show(hover.Read(), hover.Anchor);
    }

    public void Dispose() => hover.Changed -= Refresh;
}
