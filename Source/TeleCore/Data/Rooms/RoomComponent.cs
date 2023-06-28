using System.Collections.Generic;
using Verse;

namespace TeleCore;

public abstract class RoomComponent
{
    private RoomTracker _parent;
    private HashSet<RoomComponent> _adjRoomComps;

    public RoomTracker Parent => _parent;
    public IReadOnlyCollection<RoomComponent> AdjRoomComps => _adjRoomComps;
        
    //
    public Map Map => Parent.Map;
    public Room Room => Parent.Room;

    public IReadOnlyCollection<Thing> ContainedPawns => Parent.ContainedPawns;
    public bool Disbanded => Parent.IsDisbanded;
    public bool IsDoorway => Room.IsDoorway;
        
    internal void Create(RoomTracker parent)
    {
        this._parent = parent;
        _adjRoomComps = new HashSet<RoomComponent>();
    }
        
    public virtual void PostCreate(RoomTracker parent) { }
    public virtual void FinalizeMapInit() { }
        
    public virtual void Disband(RoomTracker parent, Map map) { }

    /// <summary>
    /// Runs once when the room is reused.
    /// </summary>
    public virtual void Notify_Reused() { }
    public virtual void Notify_RoofClosed() { }
    public virtual void Notify_RoofOpened() { }
    public virtual void Notify_RoofChanged() { }
    public virtual void Notify_BorderThingAdded(Thing thing) { }
    public virtual void Notify_ThingAdded(Thing thing) { }
    public virtual void Notify_ThingRemoved(Thing thing) { }
    public virtual void Notify_PawnEnteredRoom(Pawn pawn) { }
    public virtual void Notify_PawnLeftRoom(Pawn pawn) { }

    /// <summary>
    /// Runs once on initiliazation.
    /// </summary>
    public virtual void Init(RoomTracker[]? previous = null) { }
        
    /// <summary>
    /// Runs once after all components have been initialized.
    /// </summary>
    public virtual void PostInit(RoomTracker[]? previous = null) { }
    public virtual void CompTick() { }
    public virtual void OnGUI() { }
    public virtual void Draw() { }

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
        return $"{nameof(this.GetType)}[{Room.ID}]";
    }
}