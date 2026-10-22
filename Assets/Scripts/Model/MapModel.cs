using System;
using System.Collections.Generic;

public class MapNode
{
    public int Column { get; }
    public int Row { get; }
    public StationConfig Station { get; private set; }
    public List<MapNode> Next { get; } = new List<MapNode>();
    
    public void SetStation(StationConfig station) => Station = station;
    
    private readonly Dictionary<MapNode, List<TravelEventConfig>> events = new Dictionary<MapNode, List<TravelEventConfig>>();

    public MapNode(int column, int row, StationConfig station)
    {
        Column = column;
        Row = row;
        Station = station;
    }

    public IReadOnlyList<TravelEventConfig> EventsTo(MapNode next) =>
        events.TryGetValue(next, out var list) ? list : (IReadOnlyList<TravelEventConfig>)Array.Empty<TravelEventConfig>();

    public void AddEvent(MapNode next, TravelEventConfig travelEvent)
    {
        if (!events.TryGetValue(next, out var list)) events[next] = list = new List<TravelEventConfig>();
        if (!list.Contains(travelEvent)) list.Add(travelEvent);
    }
}
public class MapModel
{
    private readonly List<List<MapNode>> columns = new List<List<MapNode>>();
    public IReadOnlyList<List<MapNode>> Columns => columns;

    public MapNode Current { get; private set; }
    public IReadOnlyList<MapNode> Choices => Current.Next; 

    public event Action<MapNode> CurrentChanged;

    public MapModel(JourneyConfig config, Random random)
    {
        Generate(config, random);
        Current = columns[0][0];
    }

    public bool CanMoveTo(MapNode node) => node != null && Current.Next.Contains(node);
    
    public List<MapNode> ReachableAhead(MapNode from, int distance)
    {
        var level = new List<MapNode> { from };
        for (int step = 0; step < distance; step++)
        {
            var next = new List<MapNode>();
            foreach (var node in level)
                foreach (var n in node.Next)
                    if (!next.Contains(n)) next.Add(n);
            level = next;
        }
        return level;
    }

    public bool MoveTo(MapNode node)
    {
        if (!CanMoveTo(node)) return false;
        Current = node;
        CurrentChanged?.Invoke(node);
        return true;
    }

    // Карта — три дорожки (ряды -1, 0, 1). В каждом столбце станции на двух или трёх из них.
    // Дороги идут прямо или на соседнюю дорожку, не пересекаются; изредка добавляется съезд на соседнюю,
    // поэтому ветки то расходятся, то сходятся, и выбор дорожки что-то значит.
    private const int Lanes = 3;

    private void Generate(JourneyConfig config, Random random)
    {
        for (int c = 0; c <= config.nodeCount; c++)
        {
            var column = new List<MapNode>();
            if (c == 0) column.Add(new MapNode(c, 0, null));
            else if (c == config.nodeCount) column.Add(new MapNode(c, 0, config.finalStation));
            else
            {
                int min = Math.Max(1, config.minNodesPerColumn);
                int max = Math.Max(min, config.maxNodesPerColumn);
                int count = Math.Min(Lanes, random.Next(min, max + 1));
                bool cargo = config.cargoColumns != null && Array.IndexOf(config.cargoColumns, c) >= 0 && config.deliveryStation != null;
                foreach (int row in PickRows(count, random))
                    column.Add(new MapNode(c, row, cargo ? config.deliveryStation : PickStation(config, random)));
            }
            columns.Add(column);
        }

        for (int c = 0; c < columns.Count - 1; c++)
        {
            Link(columns[c], columns[c + 1], random);
            AddCrossRoad(columns[c], columns[c + 1], random, config.extraRoadChance);
        }
        
        foreach (var column in columns)
            foreach (var node in column)
                foreach (var next in node.Next)
                {
                    if (node.Station != null) RollEvents(node, next, node.Station.eventsOnWayFromHere, random);
                    if (next.Station != null) RollEvents(node, next, next.Station.eventsOnWayHere, random);
                }
    }

    private static List<int> PickRows(int count, Random random)
    {
        var rows = new List<int> { -1, 0, 1 };
        while (rows.Count > count) rows.RemoveAt(random.Next(rows.Count));
        return rows;
    }

    private static void RollEvents(MapNode from, MapNode to, EventChance[] chances, Random random)
    {
        if (chances == null) return;
        foreach (var entry in chances)
        {
            if (entry.travelEvent == null) continue;
            if (random.NextDouble() < entry.chance) from.AddEvent(to, entry.travelEvent);
        }
    }
    
    // Каждая станция ведёт дальше по своей дорожке, а если там пусто — на ближайшую соседнюю;
    // в каждую станцию следующего столбца приходит хотя бы одна дорога.
    private static void Link(List<MapNode> from, List<MapNode> to, Random random)
    {
        foreach (var node in from)
        {
            var target = Nearest(to, node.Row, random);
            if (target != null && !node.Next.Contains(target)) node.Next.Add(target);
        }
        foreach (var next in to)
        {
            if (HasIncoming(from, next)) continue;
            var sources = new List<MapNode>(from);
            sources.Sort((a, b) => Math.Abs(a.Row - next.Row).CompareTo(Math.Abs(b.Row - next.Row)));
            var source = sources.Find(candidate => !Crosses(from, candidate, next));
            (source ?? sources[0]).Next.Add(next);
        }
    }

    private static MapNode Nearest(List<MapNode> nodes, int row, Random random)
    {
        MapNode best = null;
        int bestDistance = int.MaxValue;
        foreach (var node in nodes)
        {
            int distance = Math.Abs(node.Row - row);
            if (distance < bestDistance || (distance == bestDistance && random.Next(2) == 0))
            {
                best = node;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static bool HasIncoming(List<MapNode> from, MapNode node)
    {
        foreach (var source in from)
            if (source.Next.Contains(node)) return true;
        return false;
    }

    // Иногда (шанс из конфига) — один съезд на соседнюю дорожку между этими столбцами, если он ничего не пересекает.
    private static void AddCrossRoad(List<MapNode> from, List<MapNode> to, Random random, double chance)
    {
        if (random.NextDouble() >= chance) return;
        var candidates = new List<(MapNode from, MapNode to)>();
        foreach (var a in from)
            foreach (var b in to)
                if (Math.Abs(a.Row - b.Row) == 1 && !a.Next.Contains(b) && !Crosses(from, a, b)) candidates.Add((a, b));
        if (candidates.Count == 0) return;
        var pick = candidates[random.Next(candidates.Count)];
        pick.from.Next.Add(pick.to);
    }

    private static bool Crosses(List<MapNode> from, MapNode a, MapNode b)
    {
        foreach (var other in from)
            foreach (var next in other.Next)
                if ((other.Row < a.Row && next.Row > b.Row) || (other.Row > a.Row && next.Row < b.Row)) return true;
        return false;
    }

    private static StationConfig PickStation(JourneyConfig config, Random random)
    {
        if (config.stations == null || config.stations.Length == 0) return null;

        int total = 0;
        foreach (var entry in config.stations) if (entry.station != null) total += entry.weight;
        if (total <= 0) return config.stations[0].station;

        int roll = random.Next(total);
        foreach (var entry in config.stations)
        {
            if (entry.station == null) continue;
            if (roll < entry.weight) return entry.station;
            roll -= entry.weight;
        }
        return config.stations[0].station;
    }
}
