using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class CompPowerPlant_Network : CompPowerPlant
    {
        private int powerTicksRemaining;
        private float internalPowerOutput;

        private Comp_NetworkStructure compNetworkStructure;
        private NetworkSubPart networkComponent;

        private List<(float, int)> valuePackage;

        public new CompProperties_NetworkStructurePowerPlant Props => (CompProperties_NetworkStructurePowerPlant)base.Props;

        public bool GeneratesPowerNow => powerTicksRemaining > 0;
        public override float DesiredPowerOutput => internalPowerOutput;

        public override void PostExposeData()
        {
            base.PostExposeData();
            //Scribe_Values.Look(ref powerProductionTicks, "powerTicks");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compNetworkStructure = parent.GetComp<Comp_NetworkStructure>();
            networkComponent = compNetworkStructure[Props.fromNetwork];

            //Naive Sort
            valuePackage = new List<(float, int)>();
            for (var i = 0; i < Props.costPerValue.Count; i++)
            {
                var costDefFloat = Props.costPerValue[i];
                for (var k = 0; k < Props.ticksPerValue.Count; k++)
                {
                    var ticksDefFloat = Props.ticksPerValue[k];
                    if (costDefFloat.def == ticksDefFloat.def)
                    {
                        valuePackage.Add((costDefFloat.value, ticksDefFloat.value));
                        break;
                    }
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            PowerTick();
        }

        private void PowerTick()
        {
            base.CompTick();
            if (!base.PowerOn)
            {
                internalPowerOutput = 0f;
                return;
            }

            if (networkComponent.Container.NotEmpty)
            {
                foreach (var storedType in networkComponent.Container.AllStoredTypes)
                {
                    for (var i = 0; i < Props.costPerValue.Count; i++)
                    {
                        var costDefFloat = Props.costPerValue[i];
                        if(storedType != costDefFloat.def) continue;
                        var package = valuePackage[i];
                        if (networkComponent.Container.TryConsume(costDefFloat.def, package.Item1))
                        {
                            powerTicksRemaining += Mathf.RoundToInt(package.Item2);
                        }
                    }
                }
            }

            if (powerTicksRemaining > 0)
            {
                internalPowerOutput = -base.Props.basePowerConsumption;
                powerTicksRemaining--;
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(base.CompInspectStringExtra());
            if (GeneratesPowerNow)
                sb.AppendLine("TR_PowerLeft".Translate(powerTicksRemaining.ToStringTicksToPeriod()));
            return sb.ToString().TrimEndNewlines();
        }
    }

    //TODO: BUFFER FOR NETWORKS; COMPONENT THAT FIRST FILLS UP BEFORE THEN PUSHING THE VALUES ON
    //TODO: BIG TASK: GRAPH BASED NETWORK; 
    public class CompProperties_NetworkStructurePowerPlant : CompProperties_Power
    {
        public NetworkDef fromNetwork;
        public List<DefFloat<NetworkValueDef>> costPerValue;
        public List<DefCount<NetworkValueDef>> ticksPerValue;
    }
}
