using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TeleCore;

public abstract class RoomComponent
{
    private HashSet<RoomComponent> _adjRoomComps;

    public RoomTracker Parent { get; private set; }

    public IReadOnlyCollection<RoomComponent> AdjRoomComps => _adjRoomComps;

    //
    public Map Map => Parent.Map;
    public Room Room => Parent.Room;

    public IReadOnlyCollection<Thing> ContainedPawns => Parent.ContainedPawns;
    public bool Disbanded => Parent.IsDisbanded;
    public bool IsDoorway => Room.IsDoorway;

    internal void Create(RoomTracker parent)
    {
        Parent = parent;
        _adjRoomComps = new HashSet<RoomComponent>();
    }

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

    public virtual void CompTick()
    {
    }

    public virtual void OnGUI()
    {
    }

    public virtual void Draw()
    {
    }

    internal void AddAdjacent<T>(T comp) where T : RoomComponent
    {
        _adjRoomComps.Add(comp);
    }

    internal void Reset()
    {
        _adjRoomComps.Clear();
    }

    internal void DisbandInternal()
    {
        _adjRoomComps.Clear();
    }

    public override string ToString()
    {
        return $"{GetType().Name}[{Room.ID}]";
    }
}