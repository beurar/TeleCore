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
        internal List<MapInformation> allMapInfos;
        internal Dictionary<Type, MapInformation> mapInfoByType;

        public NetworkMapInfo NetworkInfo => (NetworkMapInfo)mapInfoByType[typeof(NetworkMapInfo)];
        public ThingCacheMapInfo ThingCacheInfo => (ThingCacheMapInfo)mapInfoByType[typeof(ThingCacheMapInfo)];

        protected void CreateMapInfos()
        {
            var subClasses = typeof(MapInformation).AllSubclassesNonAbstract();
            allMapInfos = new List<MapInformation>(subClasses.Count);
            mapInfoByType = new Dictionary<Type, MapInformation>(subClasses.Count);
            foreach (var type in subClasses)
            {
                var mapInfo = (MapInformation)Activator.CreateInstance(type, args:this);
                allMapInfos.Add(mapInfo);
                mapInfoByType.Add(type, mapInfo);
            }
        }

        public MapComponent_TeleCore(Map map) : base(map)
        {
            StaticData.Notify_NewTibMapComp(this);
            CreateMapInfos();
        }

        public T GetMapInfo<T>() where T : MapInformation
        {
            return (T)mapInfoByType[typeof(T)];
        }

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Collections.Look();
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
                info.Draw();
            }
        }

        //Events
        public void Notify_ThingSpawned(Thing thing)
        {
            if (thing.def.HasTeleExtension(out var extension))
            {
                ThingCacheInfo.RegisterPart(extension.thingGroup, thing);
            }
        }

        public void Notify_DespawnedThing(Thing thing)
        {
            if (thing.def.HasTeleExtension(out var extension))
            {
                ThingCacheInfo.DeregisterPart(extension.thingGroup, thing);
            }
        }
    }
}
