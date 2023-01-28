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
    //TODO: YouTrack TR-6
    public class CompPowerPlant_Network : CompPowerPlant
    {
        private int powerTicksRemaining;
        private float internalPowerOutput;

        private Comp_NetworkStructure _compNetworkStructure;
        private NetworkSubPart _networkComponent;
        
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
            _compNetworkStructure = parent.GetComp<Comp_NetworkStructure>();
            _networkComponent = _compNetworkStructure[Props.fromNetwork];
        }

        public override void CompTick()
        {
            base.CompTick();
            PowerTick();
        }

        private void PowerTick()
        {
            base.CompTick();
            if (!base.PowerOn || Props.valueToTickRules.NullOrEmpty())
            {
                internalPowerOutput = 0f;
                return;
            }

            if (_networkComponent.Container.NotEmpty)
            {
                foreach (var conversion in Props.valueToTickRules)
                {
                    if (_networkComponent.Container.TotalStoredOf(conversion.valueDef) <= 0) continue;
                    if (_networkComponent.Container.TryConsume(conversion.valueDef, conversion.cost))
                    {
                        powerTicksRemaining += Mathf.RoundToInt(conversion.secondPerCost.SecondsToTicks());
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
    
    public class CompProperties_NetworkStructurePowerPlant : CompProperties_Power
    {
        public NetworkDef fromNetwork;
        public List<ValueConversion> valueToTickRules;
        
        [Obsolete]
        public List<DefFloat<NetworkValueDef>> costPerValue;
        [Obsolete]
        public List<DefCount<NetworkValueDef>> ticksPerValue;


        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            //TODO: Backwardscompatibility
            if (!costPerValue.NullOrEmpty() && !ticksPerValue.NullOrEmpty())
            {
                valueToTickRules ??= new List<ValueConversion>();
                for (var i = 0; i < costPerValue.Count; i++)
                {
                    var costDefFloat = costPerValue[i];
                    for (var k = 0; k < ticksPerValue.Count; k++)
                    {
                        var ticksDefFloat = ticksPerValue[k];
                        if (costDefFloat.def == ticksDefFloat.def)
                        {
                            valueToTickRules.Add(new ValueConversion
                            {
                                valueDef = costDefFloat.def,
                                cost = costDefFloat.value,
                                secondPerCost = ticksDefFloat.value.TicksToSeconds()
                            });
                            break;
                        }
                    }
                }
            }
        }
    }

    public struct ValueConversion
    {
        public NetworkValueDef valueDef;
        public float cost;
        public float secondPerCost;
    }
}
