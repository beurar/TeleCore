using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TeleCore.FlowCore;
using UnityEngine.Profiling;
using Verse;

namespace TeleCore
{
    public static class DefIDStack
    {
        internal static ushort _MasterID = 0; 
        internal static Dictionary<Def, ushort> _defToID;
        internal static Dictionary<ushort, Def> _idToDef;

        static DefIDStack()
        {
            _defToID = new Dictionary<Def, ushort>();
            _idToDef = new Dictionary<ushort, Def>();
        }

        public static ushort ToID(Def def)
        {
            if (_defToID.TryGetValue(def, out var id))
            {
                return id;
            }
            TLog.Warning($"Cannot find id for ({def.GetType()}){def}. Make sure to call base.PostLoad().");
            return def.index;
        }

        public static TDef ToDef<TDef>(ushort id)
            where TDef : Def
        {
            if (_idToDef.TryGetValue(id, out var def))
            {
                if(def is TDef casted)
                    return casted;
                TLog.Warning($"Cannot cast {def} to {typeof(TDef)}");
                return null;
            }
            TLog.Warning($"Cannot find Def for {id} of type {typeof(TDef)}. Make sure PostLoad calls base.PostLoad.");
            return null;
        }
        
        public static void RegisterNew<TDef>(TDef def) where TDef : Def
        {
            if (_defToID.ContainsKey(def))
            {
                TLog.Warning($"{def} is already registered.");
                return;
            }

            _defToID.Add(def, _MasterID);
            _idToDef.Add(_MasterID, def);
            _MasterID++;
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
        internal static List<PlaySettingsWorker> _playSettings;

        //Static Props
        public static WorldComp_Tele TeleWorldComp { get; internal set; }

        public static MapComponent_TeleCore TeleMapComp(int mapInt) => teleMapComps[mapInt];

        internal static List<PlaySettingsWorker> PlaySettings => _playSettings;

        static StaticData()
        {
            Notify_Reload();
            SetupPlaySettings();
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

        private static void SetupPlaySettings()
        {
            _playSettings = new List<PlaySettingsWorker>();
            foreach (var type in typeof(PlaySettingsWorker).AllSubclassesNonAbstract())
            {
                _playSettings.Add((PlaySettingsWorker)Activator.CreateInstance(type));
            }
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

        #region Def ID

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
            return DefIDStack.ToDef<TDef>(id);
            if (id > DefDatabase<TDef>.AllDefsListForReading.Count)
            {
                TLog.Warning($"Trying to access ID: {id} of {typeof(TDef)} with database of {DefDatabase<TDef>.AllDefsListForReading.Count} | {DefDatabase<FlowValueDef>.AllDefsListForReading.Count}");
                return null;
            }
            return DefDatabase<TDef>.AllDefsListForReading[id]; //DefIDStack<TDef>.GetDef(id);
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
            return DefIDStack.ToID(def);// def.index; //DefIDStack<TDef>.GetID(def);
        }

        #endregion

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
