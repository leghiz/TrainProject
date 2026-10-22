using System;
using System.Collections.Generic;
using UnityEngine;

// Карта маршрута. Станции стоят на трёх дорожках (ряд — из модели), дороги идут прямо или по диагонали
// на соседнюю дорожку. Клик по доступной станции — ехать туда, при наведении над ней всплывает погода по дороге.
// Что дальше доступных станций — неизвестно: там знак вопроса (кроме Конечной и Грузовой, куда везём посылку).
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
        var cells = new Dictionary<MapNode, Vector2Int>();
        foreach (var column in journey.Map.Columns)
            foreach (var node in column)
                cells[node] = new Vector2Int(node.Column, node.Row);

        foreach (var column in journey.Map.Columns)
            foreach (var node in column)
                foreach (var next in node.Next)
                    view.AddRoad(cells[node], cells[next]);

        foreach (var column in journey.Map.Columns)
            foreach (var node in column)
            {
                var nodeView = view.AddNode(cells[node]);
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
            var icon = station == null ? view.StartIcon : station.icon != null ? station.icon : view.UnknownIcon;
            entry.view.SetStation(icon, station != null ? station.displayName : "Start");
            var nodeState = StateOf(entry.node, map);
            entry.view.SetState(nodeState);
            entry.view.SetUnknown(!IsKnown(entry.node, nodeState));
            entry.view.SetEvents(nodeState == MapNodeState.Available ? EventIcons(map.Current, entry.node) : null);
            if (nodeState == MapNodeState.Current) view.SetCurrent(entry.view);
        }
    }

    // Видно то, что уже позади или рядом, куда можно ехать; Конечную знают все, а Грузовую-получатель — пока везём посылку.
    private bool IsKnown(MapNode node, MapNodeState state)
    {
        if (state != MapNodeState.Locked) return true;
        if (node.Column == journey.Map.Columns.Count - 1) return true;
        var delivery = journey.Delivery;
        return delivery.IsActive && node.Column == delivery.DestinationColumn && node.Station == journey.Config.deliveryStation;
    }

    private static List<Sprite> EventIcons(MapNode from, MapNode to)
    {
        var icons = new List<Sprite>();
        foreach (var travelEvent in from.EventsTo(to))
            if (travelEvent != null && travelEvent.icon != null) icons.Add(travelEvent.icon);
        return icons;
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
