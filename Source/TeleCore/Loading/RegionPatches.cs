using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace TeleCore
{
    internal static class RegionPatches
    {
        [HarmonyPatch(typeof(RegionAndRoomUpdater))]
        [HarmonyPatch("CreateOrUpdateRooms")]
        public static class CreateOrUpdateRoomsPatch
        {
            public static bool Prefix(Map ___map)
            {
                RoomTrackerUpdater.Notify_RoomUpdatePrefix(___map);
                return true;
            }

            public static void Postfix(Map ___map)
            {
                //Clear null and void rooms - RW doesnt do this
                ___map.regionGrid.allRooms.RemoveAll(r => r.Districts.Any(d => d.RegionType == RegionType.None));
                RoomTrackerUpdater.Notify_RoomUpdatePostfix(___map);
            }
        }

        [HarmonyPatch(typeof(RegionAndRoomUpdater))]
        [HarmonyPatch("NotifyAffectedDistrictsAndRoomsAndUpdateTemperature")]
        public static class NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperaturePatch
        {
            public static bool Prefix(Map ___map, List<Room> ___newRooms, HashSet<Room> ___reusedOldRooms)
            {
                if (___map is null) return true;

                RoomTrackerUpdater.Notify_SetNewRoomData(___newRooms, ___reusedOldRooms);
                return true;
            }
        }

        [HarmonyPatch(typeof(Room))]
        [HarmonyPatch("Notify_RoofChanged")]
        public static class Notify_RoofChangedPatch
        {
            public static void Postfix(Room __instance)
            {
                RoomTrackerUpdater.Notify_RoofChanged(__instance);
            }
        }

        //
        [HarmonyPatch(typeof(RegionListersUpdater))]
        [HarmonyPatch(nameof(RegionListersUpdater.RegisterInRegions))]
        public static class RegionListersUpdater_RegisterInRegionsPatch
        {
            public static void Postfix(Thing thing, Map map)
            {
                var room = GetParentRoom(thing, map);
                if (room is null) return;
                map.GetMapInfo<RoomTrackerMapInfo>().Notify_RegisterThing(thing, room);
            }
        }

        [HarmonyPatch(typeof(RegionListersUpdater))]
        [HarmonyPatch(nameof(RegionListersUpdater.DeregisterInRegions))]
        public static class RegionListersUpdater_DeregisterInRegions
        {
            public static void Postfix(Thing thing, Map map)
            {
                map.GetMapInfo<RoomTrackerMapInfo>().Notify_DeregisterThing(thing, GetParentRoom(thing, map));
            }
        }

        public static Room GetParentRoom(Thing thing, Map map)
        {
            IntVec3 position = thing.Position;
            if (!position.InBounds(map)) return null;

            Region validRegion = map.regionGrid.GetValidRegionAt_NoRebuild(position);
            if (validRegion != null && validRegion.type.Passable())
            {
                return validRegion.Room;

            }
            return null;
        }


        #region OldStuff
        /*
        [HarmonyPatch(typeof(RegionAndRoomUpdater))]
        [HarmonyPatch("RegenerateNewRegionsFromDirtyCells")]
        public static class RegionPatch
        {
            private static readonly List<Region> oldRegions = new List<Region>(); 
            private static readonly List<Region> newRegions = new List<Region>();

            public static bool Prefix(RegionAndRoomUpdater __instance)
            {
                Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                RegionGrid grid = Traverse.Create(map).Field("regionGrid").GetValue<RegionGrid>();

                oldRegions.Clear();
                map.Tiberium().TiberiumInfo.regionGrid.RemoveDirtyGrids(map.regionDirtyer.DirtyCells);
                foreach (IntVec3 dirty in map.regionDirtyer.DirtyCells)
                {
                    oldRegions.AddDistinct(grid.GetValidRegionAt(dirty));
                }
                return true;
            }

            public static void Postfix(RegionAndRoomUpdater __instance)
            {
                var instance = Traverse.Create(__instance);
                Map map = instance.Field("map").GetValue<Map>();
                var tiberium = map.Tiberium();
                var info = tiberium.TiberiumInfo;
                List<Region> regions = instance.Field("newRegions").GetValue<List<Region>>();

                newRegions.Clear();
                newRegions.AddRange(regions);

                info.regionGrid.Notify_RegionSplit(oldRegions,newRegions);
            }
        }
        */
        #endregion
    }
}
