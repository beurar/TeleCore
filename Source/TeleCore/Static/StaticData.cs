using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public static class DefIDStack<TDef>
        where TDef : Def
    {
        //private static int _DefID;
        private static Dictionary<Type, int> _MasterIDs;
        internal static Dictionary<TDef, int> DefToID;
        internal static Dictionary<int, TDef> IDToDef;

        static DefIDStack()
        {
            _MasterIDs = new Dictionary<Type, int>();
            DefToID = new Dictionary<TDef, int>();
            IDToDef = new Dictionary<int, TDef>();
        }

        private static int GetNextID(TDef def)
        {
            if (_MasterIDs.TryGetValue(def.GetType(), out var id))
            {
                return id;
            }

            _MasterIDs.Add(def.GetType(), 0);
            return 0;
        }

        private static void IncrementID(TDef def)
        {
            _MasterIDs[def.GetType()]++;
        }

        public static void RegisterDef(TDef def)
        {
            var nextID = GetNextID(def);
            if (DefToID.TryAdd(def, nextID))
            {
                IDToDef.Add(nextID, def);
                IncrementID(def);
            }
        }
    }
    
    public static class StaticData
    {
        //
        public const float DeltaTime = 1f / 60f;

        //
        internal static Dictionary<SubBuildMenuDef, SubBuildMenu> windowsByDef;
        internal static Dictionary<int, MapComponent_TeleCore> teleMapComps;
        internal static Dictionary<ThingDef, Designator> cachedDesignators;

        //

        public static MapComponent_TeleCore TeleMapComp(int mapInt) => teleMapComps[mapInt];

        static StaticData()
        {
            Notify_Reload();
        }

        internal static void ExposeStaticData()
        {
            Scribe_Collections.Look(ref windowsByDef, "windowsByDef", LookMode.Def, LookMode.Deep);
        }
        
        internal static void Notify_Reload()
        {
            TLog.Message("Clearing StaticData!");
            teleMapComps = new Dictionary<int, MapComponent_TeleCore>();
            cachedDesignators = new Dictionary<ThingDef, Designator>();
            windowsByDef = new Dictionary<SubBuildMenuDef, SubBuildMenu>();
            ActionComposition._ID = 0;
        }

        internal static void Notify_ClearingMapAndWorld()
        {
            TFind.TickManager.ClearGameTickers();
        }

        internal static void Notify_NewTeleMapComp(MapComponent_TeleCore mapComp)
        {
            teleMapComps[mapComp.map.uniqueID] = mapComp;
        }

        internal static void RegisterDefID<TDef>(TDef def)
            where TDef : Def
        {
            DefIDStack<TDef>.RegisterDef(def);
        }

        //TODO:Could be useful to others, needs documentation
        public static TDef ToDef<TDef>(this int id)
            where TDef:Def
        {
            return DefIDStack<TDef>.IDToDef[id];
        }

        public static int ToID<TDef>(this TDef def)
            where TDef : Def
        {
            return DefIDStack<TDef>.DefToID[def];
        }

        //
        public static MapComponent_TeleCore TeleCore(this Map map)
        {
            if (map != null) return teleMapComps[map.uniqueID];
            
            TLog.Warning("Map is null for TeleCore MapComp getter");
            return null;
        }
        
        public static ThingGroupCacheInfo ThingGroupCache(this Map map)
        {
            return map.TeleCore().ThingGroupCacheInfo;
        }

        public static WorldComp_Tele WorldCompTele()
        {
            return Find.World.GetComponent<WorldComp_Tele>();
        }
    }
}
