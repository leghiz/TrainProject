// Разобранный станок возвращает половину своей цены (с округлением вниз).
public static class MachineRefund
{
    public static void GiveHalf(MachineConfig config, ResourceStorageModel storage)
    {
        storage.Add(ResourceType.Wood, config.craftWood / 2);
        storage.Add(ResourceType.Scrap, config.craftScrap / 2);
        storage.Add(ResourceType.Ingot, config.craftIngots / 2);
    }
}
