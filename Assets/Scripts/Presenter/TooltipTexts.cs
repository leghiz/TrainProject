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
        Cells = wagon.Config.size.x * wagon.Config.size.y,
        Effects = ConditionEffects(wagon.Condition),
        ConditionMaterial = wagon.Condition != null ? wagon.Condition.nameMaterial : null,
        Wood = wagon.Wood,
        Scrap = wagon.Scrap
    };

    private static string Multiplier(string name, float value) =>
        name + " x" + value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    // Что даёт состояние вагона, по строке: сначала вес, потом ресурсы. У обычного — пусто.
    private static string ConditionEffects(WagonCondition condition)
    {
        if (condition == null) return "";
        var lines = new System.Collections.Generic.List<string>();
        if (Mathf.Approximately(condition.weightMultiplier, 0f)) lines.Add("Weight 0");
        else if (!Mathf.Approximately(condition.weightMultiplier, 1f)) lines.Add(Multiplier("Weight", condition.weightMultiplier));
        if (!Mathf.Approximately(condition.resourceMultiplier, 1f)) lines.Add(Multiplier("Resources", condition.resourceMultiplier));
        return string.Join("\n", lines);
    }

    // Ящик посылки: довезти до Грузовой или разобрать (держать ПКМ).
    public static TooltipContent Box(CargoBoxModel box) => new TooltipContent
    {
        IsBox = true,
        Title = "Delivery",
        Info = "Deliver or break.",
        Hp = box.Hp,
        MaxHp = box.Config.maxHp
    };

    // Посылка на станции: какой ящик, куда везти и сколько дадут.
    public static TooltipContent CargoOffer(CargoBoxConfig box, int stops) => new TooltipContent
    {
        Title = box.displayName,
        Info = $"deliver in {stops} stops\nreward: {box.rewardIngots} ingots\nweight: " +
               box.weight.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
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
        // Сварка тратит себя, пока чинит: её полоска хп и заливка квадратика — сколько заряда осталось.
        if (machine.HasCharge)
        {
            content.Hp = machine.ChargeLeft;
            content.MaxHp = config.charge;
            content.Progress = Mathf.Clamp01((float)machine.ChargeLeft / config.charge);
        }
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
        content.TraitMaterial = trait != null ? trait.nameMaterial : null;
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
