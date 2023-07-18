using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore;

public class RoomCompNeighborSet
{
    private List<RoomComponent> _neighbors;
    private List<RoomComponentLink> _links;
    
    public IReadOnlyCollection<RoomComponent> CompNeighbors => _neighbors;
    public IReadOnlyCollection<RoomComponentLink> CompLinks => _links;
    
    public void Notify_AddNeighbor<T>(T neighbor) where T : RoomComponent
    {
        _neighbors.Add(neighbor);
    }

    public void Notify_AddLink(RoomComponentLink link)
    {
        _links.Add(link);
    }
    
    public void DrawDebug(RoomComponent comp)
    {
        foreach (var portal in this._links)
        {
            GenDraw.DrawFieldEdges(portal.Connector.Position.ToSingleItemList(), Color.red);
            GenDraw.DrawFieldEdges(portal.Opposite(comp).Room.Cells.ToList(), Color.green);
        }
    }

    public void Reset()
    {
        _neighbors.Clear();
        _links.Clear();
    }
}

public abstract class RoomComponent
{
    //Component specific neighbors
    private RoomCompNeighborSet _compNeighborSet;
    
    public RoomTracker Parent { get; private set; }

    public RoomNeighborSet Neighbors => Parent.RoomNeighbors;
    public RoomCompNeighborSet CompNeighbors => _compNeighborSet;

    //
    public Map Map => Parent.Map;
    public Room Room => Parent.Room;

    public IReadOnlyCollection<Thing> ContainedPawns => Parent.ContainedPawns;
    public bool Disbanded => Parent.IsDisbanded;
    public bool IsDoorway => Room.IsDoorway;

    internal void Create(RoomTracker parent)
    {
        Parent = parent;
        _compNeighborSet = new RoomCompNeighborSet();
    }

    #region Room Linking
    
    public virtual bool IsRelevantLink(Thing thing)
    {
        return false;
    }
    
    public void Notify_AddLink(RoomComponentLink link)
    {
        _compNeighborSet.Notify_AddLink(link);
    }
    
    internal void Notify_AddNeighbor<T>(T neighbor) where T : RoomComponent
    {
        _compNeighborSet.Notify_AddNeighbor(neighbor);
    }

    #endregion

    /// <summary>
    ///     Called after all <see cref="RoomComponent" />s on the <see cref="RoomTracker" /> parent have been created.
    /// </summary>
    public virtual void PostCreate(RoomTracker parent)
    {
    }

    /// <summary>
    ///     Called after all map data has been initialized.
    ///     Runs on the main game thread, so it is safe to use Unity methods.
    /// </summary>
    public virtual void FinalizeMapInit()
    {
    }

    /// <summary>
    ///     Called when disbanded by the <see cref="RoomTracker" /> parent.
    /// </summary>
    public virtual void Disband(RoomTracker parent, Map map)
    {
    }

    /// <summary>
    ///     Triggered when the room is reused after regeneration without deletion in the game.
    /// </summary>
    public virtual void Notify_Reused()
    {
    }

    /// <summary>
    ///     Called when the room's roof has been fully constructed in the game.
    /// </summary>
    public virtual void Notify_RoofClosed()
    {
    }

    /// <summary>
    ///     Called when the room is considered to be outdoors after the roof has been changed.
    /// </summary>
    public virtual void Notify_RoofOpened()
    {
    }

    /// <summary>
    ///     Alerted if there's any change to the roof of the room (constructed or deconstructed)
    /// </summary>
    public virtual void Notify_RoofChanged()
    {
    }

    internal void Notify_HandleBorderThing(Thing thing)
    {
        Notify_BorderThingAdded(thing);
        if (IsRelevantLink(thing))
        {
            if (thing is Building_Door door) //Special edge case
            {
                var roomLink = new RoomComponentLink(thing, this, door.GetRoom().RoomTracker().GetRoomComp(this.GetType()));
                return;
            }
            
            var roomLink = new RoomComponentLink(thing, this);
            Notify_AddLink(roomLink);
            Notify_AddNeighbor(roomLink.Opposite(this));
        }
    }

    /// <summary>
    ///     Notifies the game when an object has been added to the border of the room.
    /// </summary>
    /// <param name="thing">The object that was added to the border of the room.</param>
    public virtual void Notify_BorderThingAdded(Thing thing)
    {
    }

    /// <summary>
    ///     Notifies the game when an object is added to the room.
    /// </summary>
    /// <param name="thing">The object that was added to the room.</param>
    public virtual void Notify_ThingAdded(Thing thing)
    {
    }

    /// <summary>
    ///     Called when an object is removed from the room in the game.
    /// </summary>
    /// <param name="thing">The object that was removed from the room.</param>
    public virtual void Notify_ThingRemoved(Thing thing)
    {
    }

    /// <summary>
    ///     Triggered when a character (pawn) enters the room in the game.
    /// </summary>
    /// <param name="pawn">The game character that entered the room.</param>
    public virtual void Notify_PawnEnteredRoom(Pawn pawn)
    {
    }

    /// <summary>
    ///     Triggered when a character (pawn) leaves the room in the game.
    /// </summary>
    /// <param name="pawn">The game character that left the room.</param>
    public virtual void Notify_PawnLeftRoom(Pawn pawn)
    {
    }

    /// <summary>
    ///     Runs once on initialization.
    /// </summary>
    public virtual void Init(RoomTracker?[]? previous = null)
    {
    }

    /// <summary>
    ///     Runs once after all components have been initialized.
    /// </summary>
    public virtual void PostInit(RoomTracker?[]? previous = null)
    {
    }
    
    internal void Reset()
    {
        _compNeighborSet.Reset();
    }

    internal void DisbandInternal()
    {
        _compNeighborSet.Reset();
    }

    public virtual void CompTick()
    {
    }

    public virtual void OnGUI()
    {
    }

    public virtual void Draw()
    {
    }
    
    internal void DrawDebug()
    {
        _compNeighborSet.DrawDebug(this);
    }

    public override string ToString()
    {
        return $"{GetType().Name}[{Room.ID}]";
    }
}