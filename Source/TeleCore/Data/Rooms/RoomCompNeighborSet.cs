using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore;

public class RoomCompNeighborSet : IEnumerable<RoomComponent>
{
    private List<RoomComponent> _neighbors;
    private List<RoomComponentLink> _links;
    
    public IReadOnlyCollection<RoomComponent> Neighbors => _neighbors;
    public IReadOnlyCollection<RoomComponentLink> Links => _links;
    
    public void Notify_AddNeighbor<T>(T neighbor) where T : RoomComponent
    {
        _neighbors.Add(neighbor);
    }

    public void Notify_AddLink(RoomComponentLink link)
    {
        _links.Add(link);
    }
    
    public void Reset()
    {
        _neighbors.Clear();
        _links.Clear();
    }

    public bool Contains(RoomComponent comp)
    {
        return _neighbors.Contains(comp);
    }
    
    internal void DrawDebug(RoomComponent comp)
    {
        foreach (var portal in this._links)
        {
            GenDraw.DrawFieldEdges(portal.Connector.Position.ToSingleItemList(), Color.red);
            GenDraw.DrawFieldEdges(portal.Opposite(comp).Room.Cells.ToList(), Color.green);
        }
    }
    
    public IEnumerator<RoomComponent> GetEnumerator()
    {
        return _neighbors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}