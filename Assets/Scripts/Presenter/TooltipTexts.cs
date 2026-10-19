using UnityEngine;

public static class TooltipTexts
{
    public static TooltipContent Wagon(WagonModel wagon) => new TooltipContent
    {
        Title = wagon.Config.displayName + (wagon.Condition != null ? $" ({wagon.Condition.displayName})" : ""),
        Info = $"дерево {wagon.Wood}, лом {wagon.Scrap}",
        Hp = wagon.Hp,
        MaxHp = wagon.MaxHp,
        IsWagon = true,
        Size = wagon.Config.sprite != null ? (Vector2)wagon.Config.sprite.bounds.size : Vector2.zero,
        Condition = wagon.Condition != null ? wagon.Condition.displayName : "Standard",
        ConditionInfo = wagon.Condition != null ? ConditionInfo(wagon.Condition) : null,
        Wood = wagon.Wood,
        Scrap = wagon.Scrap
    };

    private static string Multiplier(string name, float value) =>
        name + " x" + value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string ConditionInfo(WagonCondition condition)
    {
        var lines = new System.Collections.Generic.List<string>();
        if (!Mathf.Approximately(condition.resourceMultiplier, 1f)) lines.Add(Multiplier("Resources", condition.resourceMultiplier));
        if (!Mathf.Approximately(condition.weightMultiplier, 1f)) lines.Add(Multiplier("Weight", condition.weightMultiplier));
        return string.Join("\n", lines);
    }

    public static TooltipContent Workbench(WorkbenchModel workbench)
    {
        string info;
        if (workbench.IsCrafting)
            info = $"делает: {workbench.InProgress.displayName}\nосталось {Mathf.CeilToInt(workbench.TimeLeft)} с (только в пути)";
        else if (workbench.HasReady) info = $"готов: {workbench.Ready.displayName}\nтащи его на вагон";
        else info = "клик — меню крафта";
        return new TooltipContent { Title = "Верстак", Info = info };
    }

    public static TooltipContent Boiler(LocomotiveModel locomotive)
    {
        var boiler = locomotive.Boiler;
        return new TooltipContent
        {
            Title = boiler.Config.displayName,
            Info = $"уровень {locomotive.BoilerLevel}\nдерево {boiler.Loaded}/{boiler.Capacity}\nклик — загрузить" + UpgradeInfo(locomotive)
        };
    }

    private static string UpgradeInfo(LocomotiveModel locomotive)
    {
        if (!locomotive.CanUpgradeBoiler) return "\nмаксимальный уровень";
        var next = locomotive.NextLevel;
        return $"\nур. {locomotive.BoilerLevel + 1} за {locomotive.UpgradeCost} слитков:\nбак {next.tank}, скорость {next.speed:0.##}, тяга {next.traction}";
    }

    public static TooltipContent Box(CargoBoxModel box) => new TooltipContent
    {
        Title = box.Config.displayName,
        Hp = box.Hp,
        MaxHp = box.Config.maxHp,
        Info = $"награда {box.Config.rewardIngots} слитков\nдержи ПКМ — разобрать на станок"
    };
    
    public static TooltipContent Machine(MachineModel machine, bool showHp = true)
    {
        var config = machine.Config;
        string info;
        switch (config.action)
        {
            case MachineAction.Extract: info = $"добывает: {Resource(config.resource)}"; break;
            case MachineAction.Repair: info = machine.HasCharge ? $"чинит, заряд {machine.ChargeLeft}" : "чинит"; break;
            case MachineAction.Convert:
                info = $"{Resource(config.resource)} {machine.Loaded}/{Capacity(machine)} → {Resource(config.outputResource)}"; break;
            case MachineAction.Fuel: info = $"{Resource(config.resource)} {machine.Loaded}/{Capacity(machine)}"; break;
            case MachineAction.None: info = "клик — меню крафта"; break;
            default: info = ""; break;
        }
        if (machine.NeedsLoading) info += "\nклик — загрузить";
        if (!machine.IsWorking && config.action != MachineAction.None) info += "\nпростаивает";
        info += "\nдержи ПКМ — разобрать (вернёт половину)";

        var content = new TooltipContent
        {
            Title = config.displayName + (machine.Trait != null ? $" ({machine.Trait.displayName})" : ""),
            Hp = machine.Hp,
            MaxHp = showHp ? config.maxHp : 0,
            Info = info
        };
        FillMachine(ref content, config, machine.Trait, machine.Progress);
        return content;
    }

    public static TooltipContent ReadyMachine(MachineConfig config, MachineTrait trait)
    {
        var content = new TooltipContent { Title = config.displayName, Hp = config.maxHp, MaxHp = config.maxHp };
        FillMachine(ref content, config, trait, 0f);
        return content;
    }

    private static void FillMachine(ref TooltipContent content, MachineConfig config, MachineTrait trait, float progress)
    {
        content.IsMachine = true;
        content.Size = config.sprite != null ? (Vector2)config.sprite.bounds.size : new Vector2(16f, 16f);
        content.HasResource = config.action == MachineAction.Extract || config.action == MachineAction.Convert || config.action == MachineAction.Fuel;
        content.Heals = config.action == MachineAction.Repair;
        content.Resource = config.action == MachineAction.Convert ? config.outputResource : config.resource;
        content.Progress = Mathf.Clamp01(progress);
        content.TraitName = trait != null ? trait.displayName : null;
        content.TraitInfo = trait != null ? TraitInfo(trait) : null;
    }

    private static string TraitInfo(MachineTrait trait)
    {
        var lines = new System.Collections.Generic.List<string>();
        if (!Mathf.Approximately(trait.speedMultiplier, 1f)) lines.Add(Multiplier("Speed", trait.speedMultiplier));
        if (!Mathf.Approximately(trait.costMultiplier, 1f)) lines.Add(Multiplier("Wear", trait.costMultiplier));
        if (trait.rainproof) lines.Add("Rainproof");
        return string.Join("\n", lines);
    }

    private static string Capacity(MachineModel machine) => machine.Capacity > 0 ? machine.Capacity.ToString() : "∞";

    private static string Resource(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood: return "дерево";
            case ResourceType.Scrap: return "лом";
            default: return "слитки";
        }
    }
}
