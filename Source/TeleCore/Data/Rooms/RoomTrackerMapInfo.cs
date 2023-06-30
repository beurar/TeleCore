using System;
using System.Collections.Generic;
using System.Linq;
using TeleCore.Rooms.Updates;
using UnityEngine;
using Verse;

namespace TeleCore;
/* Update Flow
 * Any room-structure changed (wall)
 * Add actions for each change within the tick
 * Dicard previous actions on the same room when the room has not been yet updated
 * Update rooms in next tick based on delayed list
 * => efficiency
 */

public class RoomTrackerMapInfo : MapInformation
{
    private Dictionary<Room, RoomTracker> trackerByRoom;
    private List<RoomTracker> allTrackers;
        
    private RoomUpdater roomUpdater;
        
    public RoomUpdater Updater => roomUpdater;
        
    public Dictionary<Room, RoomTracker> AllTrackers
    {
        get => trackerByRoom;
        private set => trackerByRoom = value;
    }

    public RoomTracker this[Room room]
    {
        get
        {
            if (room == null)
            {
                TLog.Warning($"Room is null, cannot get tracker.");
                VerifyState();
                return null;
            }
            if (!trackerByRoom.ContainsKey(room))
            {
                return null;
            }
            return trackerByRoom[room];
        }
    }

    public RoomTracker this[District district]
    {
        get
        {
            if (district == null || district.Room == null)
            {
                TLog.Warning($"District({district?.ID}) or Room ({district?.Room?.ID}) is null, cannot get tracker.");
                VerifyState();
                return null;
            }
            if (!trackerByRoom.ContainsKey(district.Room))
            {
                TLog.Warning($"RoomMapInfo doesn't contain {district.Room.ID}");
                VerifyState();
                return null;
            }
            return trackerByRoom[district.Room];
        }
    }

    public RoomTrackerMapInfo(Map map) : base(map)
    {
        trackerByRoom = new();
        allTrackers = new List<RoomTracker>();
        roomUpdater = new RoomUpdater(this);
            
        TFind.TickManager.RegisterMapUITickAction(() => roomUpdater.Update());
    }

    public override void ExposeDataExtra()
    {
        //Scribe_Collections.Look(ref allTrackers, "trackers", LookMode.Deep);
    }

    public override void ThreadSafeInit()
    {
        foreach (var tracker in allTrackers)
        {
            tracker.FinalizeMapInit();
        }
    }

    //
    public override void Tick()
    {
        foreach (RoomTracker tracker in allTrackers)
        {
            tracker.RoomTick();
        }
    }

    //Data Updates
    public void Reset()
    {
        trackerByRoom.Clear();
        allTrackers.Clear();
    }

    public void SetTracker(RoomTracker tracker)
    {
        if (trackerByRoom.TryAdd(tracker.Room, tracker))
        {
            allTrackers.Add(tracker);
        }
        else
        {
            TLog.Warning($"Tried to add tracker with existing key: {tracker.Room.ID} | Outside: {tracker.IsOutside}");
        }
    }

    public void ClearTrackers()
    {
        for (int i = allTrackers.Count - 1; i >= 0; i--)
        {
            var tracker = allTrackers[i];
            trackerByRoom.Remove(tracker.Room);
            allTrackers.Remove(tracker);
        }
    }

    public void MarkDisband(RoomTracker tracker)
    {
        tracker.MarkDisbanded();
    }

    public void Disband(RoomTracker tracker)
    {
        tracker.Disband(Map);
        trackerByRoom.Remove(tracker.Room);
        allTrackers.Remove(tracker);
    }

    public void Notify_RoofChanged(Room room)
    {
        if (!trackerByRoom.TryGetValue(room, out var tracker)) return;
        tracker?.Notify_RoofChanged();
    }


    public void Notify_RegisterThing(Thing thing, Room room)
    {
        if (room is null) return;
        var tracker = this[room];
        if (tracker is null) return;
        if (!tracker.ListerThings.Contains(thing))
            tracker.Notify_RegisterThing(thing);
    }

    public void Notify_DeregisterThing(Thing thing, Room room)
    {
        if (room is null) return;

        var tracker = this[room];
        if (tracker is null) return;
        if (tracker.ListerThings.Contains(thing))
            tracker.Notify_DeregisterThing(thing);
    }

    //
    public override void UpdateOnGUI()
    {
        if (Find.CurrentMap != Map) return;
        foreach (RoomTracker tracker in allTrackers)
        {
            tracker.RoomOnGUI();
        }
    }

    public override void Update()
    {
        if (Find.CurrentMap != Map) return;
        foreach (RoomTracker tracker in allTrackers)
        {
            tracker.RoomDraw();
        }
    }

    //DEBUG
    private static char check = '✓';
    private static char fail = '❌';

    public void VerifyState()
    {
        var allRooms = Map.regionGrid.allRooms;
        var trackerCount = allTrackers.Count;
        var roomCount = allRooms.Count;
        var hitCount = 0;
        var failedTrackers = new List<RoomTracker>();
        foreach (var tracker in allTrackers)
        {
            if (allRooms.Contains(tracker.Room))
            {
                hitCount++;
            }
            else
            {
                failedTrackers.Add(tracker);
            }
        }

        var ratio = Math.Round(roomCount / (float)trackerCount, 1);
        var ratioBool = ratio == 1;
        var ratioString = $"[{roomCount}/{trackerCount}][{ratio}]{(ratioBool ? check : fail)}".Colorize(ratioBool ? Color.green : Color.red);

        var hitCountRatio = Math.Round(hitCount / (float)roomCount, 1);
        var hitBool = hitCountRatio == 1;
        var hitCountRatioString = $"[{hitCount}/{roomCount}][{hitCountRatio}]{(hitBool ? check : fail)}".Colorize(hitBool ? Color.green : Color.red);
        TLog.Message($"[Verifying RoomMapInfo] Room/Tracker Ratio: {ratioString} | HitCount Test: {hitCountRatioString}");

        if (failedTrackers.Count > 0)
        {
            TLog.Message($"Failed Tracker Count: {failedTrackers.Count}");
            TLog.Message($"Failed Trackers: {failedTrackers.Select(t => t.Room.ID).ToStringSafeEnumerable()}");
        }
    }

}