using System;
using System.Collections.Generic;

public class MapNode
{
    public int Column { get; }
    public int Row { get; }
    public StationConfig Station { get; }
    public List<MapNode> Next { get; } = new List<MapNode>();
    
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

    public bool MoveTo(MapNode node)
    {
        if (!CanMoveTo(node)) return false;
        Current = node;
        CurrentChanged?.Invoke(node);
        return true;
    }

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
                int count = random.Next(min, max + 1);
                for (int r = 0; r < count; r++) column.Add(new MapNode(c, r, PickStation(config, random)));
            }
            columns.Add(column);
        }

        for (int c = 0; c < columns.Count - 1; c++) Link(columns[c], columns[c + 1], random);
        
        foreach (var column in columns)
            foreach (var node in column)
                foreach (var next in node.Next)
                {
                    if (node.Station != null) RollEvents(node, next, node.Station.eventsOnWayFromHere, random);
                    if (next.Station != null) RollEvents(node, next, next.Station.eventsOnWayHere, random);
                }
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
    
    private static void Link(List<MapNode> from, List<MapNode> to, Random random)
    {
        int i = 0, j = 0;
        from[i].Next.Add(to[j]);
        while (i < from.Count - 1 || j < to.Count - 1)
        {
            if (i == from.Count - 1) j++;
            else if (j == to.Count - 1) i++;
            else
            {
                int step = random.Next(3); 
                if (step != 1) i++;
                if (step != 0) j++;
            }
            if (!from[i].Next.Contains(to[j])) from[i].Next.Add(to[j]);
        }
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
