using System.Collections.Generic;
using System.Linq;
using TeleCore.Data.Events;
using TeleCore.Events;
using Verse;

namespace TeleCore.Rooms.Updates;

public class RoomTrackerUpdater
{
    private readonly RoomTracker?[] _trackerGrid;

    //
    private readonly List<DelayedRoomUpdate> delayedActions = new();
    private readonly List<DelayedCacheAction> delayedCacheActions = new();
    private readonly MapInformation_Rooms parent;

    //
    private readonly List<Room> tempNewRooms = new();
    private readonly List<Room> tempReusedRooms = new();
    private int lastGameTick;

    public RoomTrackerUpdater(MapInformation_Rooms parent)
    {
        this.parent = parent;
        _trackerGrid = new RoomTracker[this.parent.Map.cellIndices.NumGridCells];
    }

    public bool IsWorking { get; private set; }

    internal void Update(bool isManual = false)
    {
        //Synchronize with game tick
        if ((!IsWorking || Find.TickManager.TicksGame <= lastGameTick) && !isManual) return;

        //
        for (var i = delayedActions.Count - 1; i >= 0; i--)
        {
            var action = delayedActions[i];
            if (action.Room == null || (action.Room.Dereferenced && action.Type != RoomChangeType.Disbanded))
            {
                delayedActions.Remove(action);
            } 
            
            switch (action.Type)
            {
                case RoomChangeType.Created:
                    var previous = action.Room.Cells.Select(c => _trackerGrid[parent.Map.cellIndices.CellToIndex(c)])
                        .Where(t => t != null).Distinct().ToArray();
                    var newTracker = new RoomTracker(action.Room);
                    action.SetTracker(newTracker);
                    action.SetPrevious(previous);
                    parent.SetTracker(newTracker);
                    break;
                case RoomChangeType.Disbanded:
                    parent.MarkDisband(action.Tracker);
                    break;
            }
        }

        //Reused
        foreach (var action in delayedActions)
        {
            if (action.Type == RoomChangeType.Reused)
            {
                action.Tracker.Reset();
                action.Tracker.Notify_Reused();
                GlobalEventHandler.OnRoomReused(new RoomChangedArgs(RoomChangeType.Reused, action.Tracker));
            }
        }

        foreach (var action in delayedActions)
        {
            if (action.Type == RoomChangeType.Disbanded)
            {
                parent.Disband(action.Tracker);
                GlobalEventHandler.OnRoomDisbanded(new RoomChangedArgs(RoomChangeType.Disbanded, action.Tracker));
            }
        }

        foreach (var action in delayedActions)
        {
            if (action.Type == RoomChangeType.Created)
            {
                action.Tracker.Init(action.Previous);
                GlobalEventHandler.OnRoomCreated(new RoomChangedArgs(RoomChangeType.Created, action.Tracker));
            }
        }

        foreach (var action in delayedActions)
        {
            if (action.Type == RoomChangeType.Created)
            {
                action.Tracker.PostInit(action.Previous);
            }
        }

        foreach (var action in delayedCacheActions)
        {
            _trackerGrid[action.Index] = null;
        }

        //
        IsWorking = false;
        delayedActions.Clear();
    }

    //Cache cells around recently spawned and despawned structures
    internal void Notify_CacheDirtyCell(IntVec3 cell, Region region)
    {
        _trackerGrid[parent.Map.cellIndices.CellToIndex(cell)] = parent[region.Room];
    }

    internal void Notify_ResetDirtyCell(IntVec3 cell)
    {
        //_trackerGrid[parent.Map.cellIndices.CellToIndex(cell)] = null;
        delayedCacheActions.Add(new DelayedCacheAction(cell, parent.Map.cellIndices.CellToIndex(cell)));
    }

    internal void Notify_UpdateStarted()
    {
        if (IsWorking)
        {
            for (var i = delayedActions.Count - 1; i >= 0; i--)
            {
                var delayedRoomUpdate = delayedActions[i];
                if (!(delayedRoomUpdate.Room?.Dereferenced ?? true)) continue;
                delayedActions.Remove(delayedRoomUpdate);
            }

            return;
        }

        IsWorking = true;
    }

    internal void Notify_NewRooms(List<Room> newRoomsData, HashSet<Room> reusedRoomsData)
    {
        tempNewRooms.AddRange(newRoomsData);
        tempReusedRooms.AddRange(reusedRoomsData);
    }

    internal void Notify_Finalize()
    {
        //Compare with new generated rooms
        foreach (var newAddedRoom in tempNewRooms)
            delayedActions.Add(new DelayedRoomUpdate(RoomChangeType.Created, newAddedRoom));

        foreach (var tracker in parent.AllTrackers.Values)
        {
            if (tempReusedRooms.Contains(tracker.Room))
                delayedActions.Add(new DelayedRoomUpdate(RoomChangeType.Reused, tracker));

            if (tracker.Room.Dereferenced)
                delayedActions.Add(new DelayedRoomUpdate(RoomChangeType.Disbanded, tracker));
        }

        //
        lastGameTick = Find.TickManager.TicksGame;
        tempNewRooms.Clear();
        tempReusedRooms.Clear();

        //During map generation, update immediately
        if (Current.ProgramState != ProgramState.Playing) Update(true);
    }
}