using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class MapComponent_TeleCore : MapComponent
    {
        internal List<MapInformation> allMapInfos = new();
        internal Dictionary<Type, MapInformation> mapInfoByType = new();

        public NetworkMapInfo NetworkInfo => (NetworkMapInfo)mapInfoByType[typeof(NetworkMapInfo)];
        public ThingCacheMapInfo ThingCacheInfo => (ThingCacheMapInfo)mapInfoByType[typeof(ThingCacheMapInfo)];

        public MapComponent_TeleCore(Map map) : base(map)
        {
            StaticData.Notify_NewTibMapComp(this);
            FillMapInformations();
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
                    TLog.Error($"Could not instantiate a MapInformation of type {type}: {ex}");
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

        public void ThreadSafeFinalize()
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

        //Events
        public void Notify_ThingSpawned(Thing thing)
        {
            if (thing.def.HasTeleExtension(out var extension))
            {
                foreach (var group in extension.thingGroups)
                {
                    ThingCacheInfo.RegisterPart(group, thing);
                }
            }
        }

        public void Notify_DespawnedThing(Thing thing)
        {
            if (thing.def.HasTeleExtension(out var extension))
            {
                foreach (var group in extension.thingGroups)
                {
                    ThingCacheInfo.DeregisterPart(group, thing);
                }
            }
        }
    }
}
