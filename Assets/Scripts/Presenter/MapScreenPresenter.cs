using System;
using System.Collections.Generic;
using UnityEngine;

public class MapScreenPresenter : IDisposable
{
    private readonly JourneyModel journey;
    private readonly MapScreenView view;
    private readonly List<(MapNode node, MapNodeView view, Action onClick)> nodes =
        new List<(MapNode, MapNodeView, Action)>();

    public MapScreenPresenter(JourneyModel journey, MapScreenView view)
    {
        this.journey = journey;
        this.view = view;

        Build();
        Refresh(journey.State);
        journey.StateChanged += Refresh;
    }

    private void Build()
    {
        var columns = journey.Map.Columns;
        var positions = new Dictionary<MapNode, Vector2>();
        for (int c = 0; c < columns.Count; c++)
        {
            float x = columns.Count > 1 ? (float)c / (columns.Count - 1) : 0.5f;
            var column = columns[c];
            for (int r = 0; r < column.Count; r++)
                positions[column[r]] = new Vector2(x, (r + 1f) / (column.Count + 1f));
        }

        foreach (var column in columns)
            foreach (var node in column)
                foreach (var next in node.Next)
                {
                    Vector2 from = positions[node], to = positions[next];
                    view.AddLine(from, to);
                    var events = node.EventsTo(next);
                    for (int i = 0; i < events.Count; i++)
                        view.AddEvent(Vector2.Lerp(from, to, (i + 1f) / (events.Count + 1f)))
                            .Setup(events[i].icon, events[i].displayName);
                }

        foreach (var column in columns)
            foreach (var node in column)
            {
                var nodeView = view.AddNode(positions[node]);
                var target = node;
                Action onClick = () => journey.Travel(target);
                nodeView.Clicked += onClick;
                nodes.Add((node, nodeView, onClick));
            }
    }

    private void Refresh(JourneyState state)
    {
        if (state != JourneyState.Map)
        {
            view.Hide();
            return;
        }
        view.Show();
        
        var map = journey.Map;
        foreach (var entry in nodes)
        {
            var station = entry.node.Station;
            entry.view.SetStation(station != null ? station.icon : null, station != null ? station.displayName : "Старт");
            entry.view.SetState(StateOf(entry.node, map));
        }
    }

    private static MapNodeState StateOf(MapNode node, MapModel map)
    {
        if (node == map.Current) return MapNodeState.Current;
        if (map.CanMoveTo(node)) return MapNodeState.Available;
        return node.Column < map.Current.Column ? MapNodeState.Passed : MapNodeState.Locked;
    }

    public void Dispose()
    {
        journey.StateChanged -= Refresh;
        foreach (var entry in nodes) entry.view.Clicked -= entry.onClick;
        nodes.Clear();
    }
}
