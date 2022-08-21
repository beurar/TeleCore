using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using Verse;

namespace TeleCore
{
    public class WorldComp_Tele : WorldComponent
    {
        //Discovery
        internal DiscoveryTable discoveryTable;

        public WorldComp_Tele(World world) : base(world)
        {
            GenerateInfos();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref discoveryTable, "DiscoveryTable");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GenerateInfos();
            }
        }


        private void GenerateInfos()
        {
            discoveryTable ??= new DiscoveryTable();
        }
    }
}
