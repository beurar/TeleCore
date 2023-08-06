using System.Collections.Generic;
using TeleCore.Data.Events;
using TeleCore.Events;
using Verse;

namespace TeleCore.Static;

public static class QuickAccess
{
    private static Dictionary<Thing, RoomTracker> _roomTrackerByThing;
    
    static QuickAccess()
    {
        _roomTrackerByThing = new();

        GlobalEventHandler.ThingSpawned += HandleThingSpawn;
        GlobalEventHandler.ThingDespawned += HandleThingDespawn;
        
        GlobalEventHandler.RoomCreated += HandleRoomCreated;
    }

    private static void HandleRoomCreated(RoomChangedArgs args)
    {
        args.RoomTracker.Room.Regions[0].ListerThings.AllThings.ForEach(thing =>
        {
            if (_roomTrackerByThing.ContainsKey(thing))
            {
                _roomTrackerByThing[thing] = args.RoomTracker;
            }
            else
            {
                _roomTrackerByThing.Add(thing, args.RoomTracker);
            }
        });
    }

    private static void HandleThingDespawn(ThingStateChangedEventArgs args)
    {
        
    }

    private static void HandleThingSpawn(ThingStateChangedEventArgs args)
    {
        
    }
}