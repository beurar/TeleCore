using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public static class StaticData
    {
        //
        public const float DeltaTime = 1f / 60f;

        //
        internal static Dictionary<int, MapComponent_TeleCore> TeleMapComps;
        internal static Dictionary<ThingDef, Designator> CachedDesignators;

        internal static MapComponent_TeleCore TeleMapComp(int mapInt) => TeleMapComps[mapInt];

        static StaticData()
        {
            Notify_Reload();
        }

        internal static void Notify_Reload()
        {
            TLog.Message("Clearing StaticData!");
            TeleMapComps = new Dictionary<int, MapComponent_TeleCore>();
            CachedDesignators = new Dictionary<ThingDef, Designator>();
        }

        internal static void Notify_ClearingMapAndWorld()
        {
            TFind.TickManager.ClearGameTickers();
        }

        internal static void Notify_NewTibMapComp(MapComponent_TeleCore mapComp)
        {
            TeleMapComps[mapComp.map.uniqueID] = mapComp;
        }

        internal static MapComponent_TeleCore TeleCore(this Map map)
        {
            if (map == null)
            {
                TLog.Warning("Map is null for TeleCore MapComp getter");
                return null;
            }
            return TeleMapComps[map.uniqueID];
        }
    }
}
