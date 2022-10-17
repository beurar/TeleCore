using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class MapComponent_TeleCore : MapComponent
    {
        private List<MapInformation> allMapInfos = new();
        private readonly Dictionary<Type, MapInformation> mapInfoByType = new();
        
        public NetworkMapInfo NetworkInfo { get; }
        public ThingGroupCacheInfo ThingGroupCacheInfo { get; }
        public ThingTrackerInfo ThingTrackerInfo { get; }
        
        public MapComponent_TeleCore(Map map) : base(map)
        {
            StaticData.Notify_NewTibMapComp(this);
            FillMapInformations();
            
            //
            NetworkInfo = (NetworkMapInfo)mapInfoByType[typeof(NetworkMapInfo)];
            ThingGroupCacheInfo = (ThingGroupCacheInfo)mapInfoByType[typeof(ThingGroupCacheInfo)];
            ThingTrackerInfo = (ThingTrackerInfo)mapInfoByType[typeof(ThingTrackerInfo)];
        }

        public T GetMapInfo<T>() where T : MapInformation
        {
            return (T)mapInfoByType[typeof(T)];
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref allMapInfos, "mapInfos", LookMode.Deep, map);

            FillMapInformations();
        }

        private void FillMapInformations()
        {
            allMapInfos.RemoveAll(m => m == null);
            foreach (Type type in typeof(MapInformation).AllSubclassesNonAbstract())
            {
                if (allMapInfos.Any(m => m.GetType() == type)) continue;

                try
                {
                    MapInformation item = (MapInformation)Activator.CreateInstance(type, map);
                    allMapInfos.Add(item);
                }
                catch (Exception ex)
                {
                    TLog.Error($"Could not instantiate a MapInformation of type {type}:\n{ex}");
                }
            }
            mapInfoByType.Clear();
            foreach (var mapInfo in allMapInfos)
            {
                mapInfoByType.Add(mapInfo.GetType(), mapInfo);
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            for (var i = 0; i < allMapInfos.Count; i++)
            {
                var info = allMapInfos[i];
                info.InfoInit();
            }
            
            //
            LongEventHandler.QueueLongEvent(ThreadSafeFinalize, string.Empty, false, null, false);
        }

        private void ThreadSafeFinalize()
        {
            for (var i = 0; i < allMapInfos.Count; i++)
            {
                var info = allMapInfos[i];
                info.ThreadSafeInit();
            }
        }

        public override void MapGenerated()
        {
            for (var i = 0; i < allMapInfos.Count; i++)
            {
                var info = allMapInfos[i];
                info.OnMapGenerated();
            }
        }

        //Updates
        public override void MapComponentTick()
        {
            for (var i = 0; i < allMapInfos.Count; i++)
            {
                var info = allMapInfos[i];
                info.Tick();
            }
        }

        public override void MapComponentOnGUI()
        {
            for (var i = 0; i < allMapInfos.Count; i++)
            {
                var info = allMapInfos[i];
                info.UpdateOnGUI();
            }
        }

        public override void MapComponentUpdate()
        {
            for (var i = 0; i < allMapInfos.Count; i++)
            {
                var info = allMapInfos[i];
                info.Update();
            }
        }
        
        public void CustomMapUpdate()
        { 
            for (var i = 0; i < allMapInfos.Count; i++)
            {
                var info = allMapInfos[i];
                info.CustomUpdate();
            }
        }

        //Events
        public void Notify_ThingSpawned(Thing thing)
        {
            try
            {
                //
                ThingTrackerInfo.Notify_RegisterThing(thing);
            }
            catch (Exception ex)
            {
                TLog.Error($"Error trying to register spawned thing: {thing}\n{ex.Message}");
            }
            
            //
            if (thing.def.HasTeleExtension(out var extension))
            {
                foreach (var group in extension.thingGroups.groups)
                {
                    ThingGroupCacheInfo.RegisterPart(group, thing);
                }
            }
        }

        public void Notify_DespawnedThing(Thing thing)
        {
            try
            {
                ThingTrackerInfo.Notify_DeregisterThing(thing);
            }
            catch (Exception ex)
            {
                TLog.Error($"Error trying to deregister despawned thing: {thing}\n{ex.Message}");
            }

            //
            if (thing.def.HasTeleExtension(out var extension))
            {
                foreach (var group in extension.thingGroups.groups)
                {
                    ThingGroupCacheInfo.DeregisterPart(group, thing);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify_ThingSentSignal(Thing thing, string signal)
        {
            try
            {
                ThingTrackerInfo.Notify_ThingStateChanged(thing, signal);
            }
            catch (Exception ex)
            {
                TLog.Error($"Error trying to send signal on thing: {thing}\n{ex.Message}");
            }
        }
    }
}
