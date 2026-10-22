using System;

// Клик по плавильне — меню у её тултипа: сколько лома в ней лежит и кнопка, которая подкидывает лом со склада.
// Пока меню открыто, тултип плавильни (что плавится и сколько осталось) не пропадает, даже если мышь ушла к кнопке.
public class FoundryMenuPresenter : IDisposable
{
    private readonly MachineMenuModel menu;
    private readonly JourneyModel journey;
    private readonly BuildModel build;
    private readonly ResourceStorageModel storage;
    private readonly HoverModel hover;
    private readonly FoundryMenuView view;

    public FoundryMenuPresenter(MachineMenuModel menu, JourneyModel journey, BuildModel build, ResourceStorageModel storage,
                                HoverModel hover, FoundryMenuView view)
    {
        this.menu = menu;
        this.journey = journey;
        this.build = build;
        this.storage = storage;
        this.hover = hover;
        this.view = view;

        view.Hide();
        menu.Changed += OnChanged;
        view.AddClicked += AddScrap;
        view.CloseClicked += menu.Close;
        build.SelectedChanged += OnSelectedChanged;
        journey.StateChanged += OnStateChanged;
    }

    private void OnChanged()
    {
        if (!menu.IsOpen)
        {
            view.Hide();
            hover.Clear(this);
            return;
        }
        view.Show();
        Pin();
        Refresh();
    }

    // Каждый кадр, пока меню открыто: лом плавится, склад меняется — кнопка то доступна, то нет.
    public void Tick()
    {
        if (!menu.IsOpen) return;
        if (!hover.HasTarget) Pin();
        Refresh();
    }

    private void Pin()
    {
        var machine = menu.Machine;
        hover.Show(this, () => TooltipTexts.Machine(machine), menu.Anchor);
    }

    private void AddScrap()
    {
        if (!menu.IsOpen) return;
        menu.Machine.Load(storage, view.ScrapPerClick);
        Refresh();
    }

    private void Refresh()
    {
        var machine = menu.Machine;
        view.SetLoaded(machine.Loaded);
        bool roomLeft = machine.Capacity <= 0 || machine.Loaded < machine.Capacity;
        view.SetCanAdd(roomLeft && storage.Has(machine.Config.resource, 1));
    }

    private void OnSelectedChanged(MachineConfig machine)
    {
        if (!build.IsEmpty) menu.Close();
    }

    private void OnStateChanged(JourneyState state) => menu.Close();

    public void Dispose()
    {
        menu.Changed -= OnChanged;
        view.AddClicked -= AddScrap;
        view.CloseClicked -= menu.Close;
        build.SelectedChanged -= OnSelectedChanged;
        journey.StateChanged -= OnStateChanged;
        hover.Clear(this);
    }
}
