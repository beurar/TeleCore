using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore;

public class CompPowerPlant_Network : CompPowerPlant
{
    private int powerTicksRemaining;
    private float internalPowerOutput = 0f;

    private Comp_NetworkStructure _compNetworkStructure;
    private NetworkSubPart _networkComponent;

    public new CompProperties_NetworkStructurePowerPlant Props =>
        (CompProperties_NetworkStructurePowerPlant) base.Props;

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
        if (!PowerOn || Props.valueToTickRules.NullOrEmpty())
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
                    powerTicksRemaining += Mathf.RoundToInt(conversion.seconds.SecondsToTicks());
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
}

public class ValueConversion
{
    public NetworkValueDef valueDef;
    public float cost;
    public float seconds;
}

