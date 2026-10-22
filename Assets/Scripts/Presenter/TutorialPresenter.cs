using System;

// Обучение в самом начале. Поезд сразу едет на первый узел карты, но путь стоит (ни мы, ни буря не двигаемся).
// Подсвечивается верстак → в меню станок, который добывает дерево (на старте хватает только на него) →
// ставим его в вагон → он добывает немного дерева и замирает → подсвечивается коптильня → кнопка «подкинуть дров».
// Подкинули — путь пошёл, камера отъезжает. Пока шаг не сделан, игра ждёт: время стоит, лишнего не добыть.
public class TutorialPresenter : IDisposable
{
    private enum Step { Workbench, Recipe, Craft, Mine, Boiler, Feed, Storm, Back, Done }

    // Сколько секунд камера смотрит на бурю и сколько едет обратно к поезду.
    private const float StormSeconds = 2.2f;
    private const float BackSeconds = 1.2f;

    private readonly JourneyModel journey;
    private readonly WorkbenchModel workbench;
    private readonly BuildModel build;
    private readonly ResourceStorageModel storage;
    private readonly LocomotiveView locomotiveView;
    private readonly TrainView trainView;
    private readonly TutorialArrowView arrow;
    private readonly CameraRig cameraRig;
    private readonly UnityEngine.Transform storm;
    private float timer;
    private readonly WorkbenchPanelView workbenchPanel;
    private readonly BoilerMenuView boilerMenu;
    private readonly int woodToMine;
    private readonly Action finished;
    private readonly MachineConfig miner;

    private Step step = Step.Workbench;
    private int woodBefore;
    private int fuelBefore;

    public TutorialPresenter(JourneyModel journey, WorkbenchModel workbench, BuildModel build, ResourceStorageModel storage,
                             TrainView trainView, WorkbenchPanelView workbenchPanel, BoilerMenuView boilerMenu,
                             TutorialArrowView arrow, CameraRig cameraRig, UnityEngine.Transform storm,
                             int woodToMine, Action finished)
    {
        this.cameraRig = cameraRig;
        this.storm = storm;
        locomotiveView = trainView.Locomotive;
        this.trainView = trainView;
        this.arrow = arrow;
        this.journey = journey;
        this.workbench = workbench;
        this.build = build;
        this.storage = storage;
        this.workbenchPanel = workbenchPanel;
        this.boilerMenu = boilerMenu;
        this.woodToMine = woodToMine;
        this.finished = finished;

        foreach (var recipe in workbench.Recipes)
            if (recipe != null && recipe.action == MachineAction.Extract && recipe.resource == ResourceType.Wood) miner = recipe;

        var next = journey.Map.Current.Next;
        if (miner == null || next.Count == 0 || !journey.Travel(next[next.Count / 2]))
        {
            step = Step.Done;
            ReleaseFocus();
            return;
        }
        journey.Frozen = true;
        TutorialFocus.Active = true;
    }

    // Стрелка над целью, нажать можно только её. null — ждём, нажимать нечего.
    private void Focus(UnityEngine.Transform target)
    {
        TutorialFocus.Active = true;
        TutorialFocus.Target = target;
        if (arrow != null) arrow.Point(target);
    }

    public void Tick()
    {
        switch (step)
        {
            case Step.Workbench:
                Focus(locomotiveView.WorkbenchPoint);
                locomotiveView.HighlightWorkbench(true);
                if (workbenchPanel.IsOpen) step = Step.Recipe;
                if (workbench.IsCrafting || workbench.HasReady) step = Step.Craft;
                break;

            case Step.Recipe:
                Focus(MinerRow());
                SetRowHighlight(true);
                if (workbench.IsCrafting || workbench.HasReady)
                {
                    SetRowHighlight(false);
                    step = Step.Craft;
                }
                else if (!workbenchPanel.IsOpen)
                {
                    SetRowHighlight(false);
                    step = Step.Workbench;
                }
                break;

            case Step.Craft:
                if (build.Selected == miner && trainView.Wagons.Count > 0) Focus(trainView.Wagons[0].transform);
                else Focus(workbench.HasReady ? locomotiveView.ReadyIconPoint : null);
                if (!MinerPlaced()) break;
                woodBefore = storage.Get(ResourceType.Wood);
                journey.MachinesWork = true;
                step = Step.Mine;
                break;

            case Step.Mine:
                Focus(null);
                if (storage.Get(ResourceType.Wood) - woodBefore < woodToMine) break;
                journey.MachinesWork = false;
                step = Step.Boiler;
                break;

            case Step.Boiler:
                Focus(locomotiveView.BoilerPoint);
                locomotiveView.HighlightBoiler(true);
                if (!boilerMenu.IsOpen) break;
                fuelBefore = Fuel();
                step = Step.Feed;
                break;

            case Step.Feed:
                Focus(boilerMenu.AddButtonPoint);
                boilerMenu.HighlightAdd(true);
                if (Fuel() > fuelBefore)
                {
                    ShowStorm();
                    break;
                }
                if (!boilerMenu.IsOpen)
                {
                    boilerMenu.HighlightAdd(false);
                    step = Step.Boiler;
                }
                break;

            case Step.Storm:
                timer += UnityEngine.Time.unscaledDeltaTime;
                if (timer < StormSeconds) break;
                timer = 0f;
                if (cameraRig != null) cameraRig.LookAt(locomotiveView.transform);
                step = Step.Back;
                break;

            case Step.Back:
                timer += UnityEngine.Time.unscaledDeltaTime;
                if (timer < BackSeconds) break;
                if (cameraRig != null) cameraRig.LookAt(null);
                Finish();
                break;
        }
    }

    // Заправились: меню закрывается, камера едет показать бурю позади поезда, потом обратно.
    private void ShowStorm()
    {
        boilerMenu.HighlightAdd(false);
        Focus(null);
        if (cameraRig == null || storm == null)
        {
            Finish();
            return;
        }
        timer = 0f;
        cameraRig.LookAt(storm);
        step = Step.Storm;
    }

    private bool MinerPlaced()
    {
        foreach (var wagon in journey.Train.Wagons)
            foreach (var machine in wagon.Machines)
                if (machine.Config == miner) return true;
        return false;
    }

    private int Fuel()
    {
        var boiler = journey.Train.Locomotive.Boiler;
        return boiler != null ? boiler.Loaded : 0;
    }

    private UnityEngine.Transform MinerRow()
    {
        foreach (var row in workbenchPanel.Rows)
            if (row.Recipe == miner) return row.transform;
        return null;
    }

    private void SetRowHighlight(bool value)
    {
        foreach (var row in workbenchPanel.Rows)
            if (row.Recipe == miner) row.SetHighlighted(value);
    }

    private void Finish()
    {
        step = Step.Done;
        boilerMenu.HighlightAdd(false);
        SetRowHighlight(false);
        if (!workbenchPanel.IsOpen) locomotiveView.HighlightWorkbench(false);
        journey.MachinesWork = false;
        journey.Frozen = false;
        ReleaseFocus();
        finished?.Invoke();
    }

    private void ReleaseFocus()
    {
        TutorialFocus.Active = false;
        TutorialFocus.Target = null;
        if (arrow != null) arrow.Point(null);
    }

    public void Dispose()
    {
        ReleaseFocus();
        if (step == Step.Done) return;
        journey.Frozen = false;
        journey.MachinesWork = false;
    }
}
