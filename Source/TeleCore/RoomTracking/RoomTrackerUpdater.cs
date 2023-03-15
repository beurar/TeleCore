using System.Collections.Generic;
using System.Linq;
using TeleCore.Memory;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal static class RoomTrackerUpdater
    {
        private static readonly List<RoomTracker> ExistingTrackers = new();

        private static readonly List<RoomTracker> NewTrackers = new();
        private static readonly List<RoomTracker> NewExistingTrackers = new();
        private static readonly List<RoomTracker> ReusedTrackers = new();

        private static readonly List<Room> ReusedOldRooms = new();
        private static readonly List<Room> NewRooms = new();

        static RoomTrackerUpdater()
        {

        }

        //Notify roof change on this room group
        public static void Notify_RoofChanged(Room room)
        {
            room.Map.GetMapInfo<RoomTrackerMapInfo>().Notify_RoofChanged(room);
        }

        //Initial step of room update - setting known data
        public static void Notify_RoomUpdatePrefix(Map map)
        {
            //TLog.Message($"RoomComp Update Prefix on {map} | {map?.uniqueID}");
            var roomInfo = map.GetMapInfo<RoomTrackerMapInfo>();
            ExistingTrackers.Clear();

            //TLog.Message($"RoomInfo?: {roomInfo != null} | {roomInfo.AllTrackers?.Values?.Count}");
            ExistingTrackers.AddRange(roomInfo.AllTrackers.Values.ToList());
            roomInfo.ClearTrackers();

            ReusedOldRooms.Clear();
            NewRooms.Clear();
        }

        //Passing newly generated rooms 
        public static void Notify_SetNewRoomData(List<Room> newRoomsData, HashSet<Room> reusedRoomsData)
        {
            NewRooms.AddRange(newRoomsData); //newRooms.ListFullCopy()
            ReusedOldRooms.AddRange(reusedRoomsData); //reusedRooms.ToList();
        }

        //Last step, comparing known data, with new generated rooms
        public static void Notify_RoomUpdatePostfix(Map map)
        {
            //using var g = new GarbageMan();
            TProfiler.Begin("RoomUpdatePostfix");
            
            //Get all rooms after vanilla updater finishes
            var roomInfo = map.GetMapInfo<RoomTrackerMapInfo>();
            var allRooms = map.regionGrid.allRooms;

            //Iterate through all rooms
            foreach (var newRoom in allRooms)
            {
                if (Enumerable.Any(NewTrackers, t => t.Room == newRoom)) continue;
                //Compare if any known rooms still exist
                var tracker = ExistingTrackers.Find(t => t.Room == newRoom);
                if (tracker != null)
                {
                    //Notify Tracker Changed
                    if (ReusedOldRooms.Contains(tracker.Room))
                    {
                        ReusedTrackers.Add(tracker);
                    }
                    NewExistingTrackers.Add(tracker);
                    continue;
                }

                //Compare with new generated rooms
                foreach (var newAddedRoom in NewRooms)
                {
                    if (newRoom == newAddedRoom)
                    {
                        var newTracker = new RoomTracker(newAddedRoom);
                        NewTrackers.Add(newTracker);
                        break;
                    }
                }
            }
            
            TProfiler.Check("1");

            //Compare old rooms with new rooms to disband unused ones
            var allActiveTrackers = NewTrackers.Concat(NewExistingTrackers).ToList();
            var disbanded = ExistingTrackers.Except(allActiveTrackers).ToList();
            
            TProfiler.Check("2");
            
            foreach (var tracker in disbanded)
            {
                roomInfo.MarkDisband(tracker);
            }

            TProfiler.Check("3");

            //Finalize Addition
            foreach (var tracker in allActiveTrackers)
            {
                roomInfo.SetTracker(tracker);
            }

            TProfiler.Check("4");
            
            //
            foreach (var tracker in ReusedTrackers.Concat(NewTrackers))
            {
                tracker.Reset();
            }
            
            TProfiler.Check("5");

            //
            foreach (var tracker in ReusedTrackers)
            {
                tracker.Notify_Reused();
            }
            
            TProfiler.Check("6");

            //
            foreach (var tracker in disbanded)
            {
                roomInfo.Disband(tracker);
            }

            TProfiler.Check("7");
            
            foreach (var tracker in NewTrackers)
            {
                tracker.PreApply();
            }
            
            TProfiler.Check("8");

            foreach (var tracker in NewTrackers)
            {
                tracker.FinalizeApply();
            }
            
            TProfiler.Check("9");

            NewTrackers.Clear();
            ReusedTrackers.Clear();
            NewExistingTrackers.Clear();
            ExistingTrackers.Clear();
            
            TProfiler.End();
        }
    }
}
