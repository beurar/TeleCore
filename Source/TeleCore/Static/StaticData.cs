using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace TeleCore
{
    public class DefIDStack<TDef> where TDef : Def
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetID(TDef def)
        {
            return def.index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDef GetDef(ushort index)
        {
            return DefDatabase<TDef>.AllDefsListForReading[index];
        }
        
        /*public static string StackReadout 
        {
            get
            {
                return $"IDs: {_MasterIDs.ToStringSafeEnumerable()}\n" +
                       $"DefToID: {DefToID.ToStringSafeEnumerable()}\n" +
                       $"IDToDef: {IDToDef.ToStringSafeEnumerable()}";
            }
        }

        public static int Count => _MasterIDs.Count;

        private static ushort GetNextID(TDef def)
        {
            def.index
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
            if (!DefToID.ContainsKey(def))
            {
                DefToID.Add(def, nextID);
                IDToDef.Add(nextID, def);
                IncrementID(def);
            }
        }*/
    }
    
    public static class StaticData
    {
        //
        public const float DeltaTime = 1f / 60f;

        //
        internal static Dictionary<SubBuildMenuDef, SubBuildMenu> windowsByDef;
        internal static Dictionary<int, MapComponent_TeleCore> teleMapComps;
        internal static Dictionary<ThingDef, Designator> cachedDesignators;

        //Static Props
        public static WorldComp_Tele TeleWorldComp { get; internal set; }

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
            //TLog.Message("Clearing StaticData!");
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

        internal static void Notify_NewTeleWorldComp(WorldComp_Tele worldComp)
        {
            TeleWorldComp ??= worldComp;
        }

        //TODO:Could be useful to others, needs documentation
        
        /// <summary>
        /// Returns the relative Def object assignable to the provided ID.
        /// </summary>
        /// <param name="id">The ID of the Def.</param>
        /// <typeparam name="TDef">The Def type to search through.</typeparam>
        /// <returns>A unique Def instance as identified by the id.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDef ToDef<TDef>(this ushort id)
            where TDef:Def
        {
            return DefIDStack<TDef>.GetDef(id);
        }

        /// <summary>
        /// Turns a Def to its relative ID.
        /// <para>Can be used to handle Defs in a more lightweight way without assigning references to objects.</para>
        /// </summary>
        /// <param name="def">The Def instance.</param>
        /// <typeparam name="TDef">The Def type to assign the ID from.</typeparam>
        /// <returns>A unique ID for the Def instance of the given Def type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToID<TDef>(this TDef def)
            where TDef : Def
        {
            return DefIDStack<TDef>.GetID(def);
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
