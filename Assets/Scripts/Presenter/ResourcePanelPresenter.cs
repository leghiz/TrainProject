using System;

public class ResourcePanelPresenter : IDisposable
{
    private readonly ResourceStorageModel storage;
    private readonly ResourcePanelView view;

    public ResourcePanelPresenter(ResourceStorageModel storage, ResourcePanelView view)
    {
        this.storage = storage;
        this.view = view;

        Refresh();
        storage.Changed += Refresh;
    }

    private void Refresh() =>
        view.Set(storage.Get(ResourceType.Wood), storage.Get(ResourceType.Scrap), storage.Get(ResourceType.Ingot));

    public void Dispose() => storage.Changed -= Refresh;
}
