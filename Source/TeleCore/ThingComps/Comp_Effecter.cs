using System.Collections.Generic;
using System.Numerics;
using RimWorld;
using Verse;

namespace TeleCore;

public class Comp_Effecter : ThingComp
{
    private Effecter _effecter;
    
    public CompProperties_Effecter Props => (CompProperties_Effecter) base.props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _effecter = new Effecter(Props.effecterDef);
    }

    public override void CompTick()
    {
        base.CompTick();
        foreach (var vector3 in CurrentMoteThrowerOffsetsFor())
        {
            _effecter.EffectTick(vector3, null);   
        }
    }
    
    //
    public IEnumerable<Vector3> CurrentMoteThrowerOffsetsFor()
    {
        foreach (var vector in Props.positionOffSets[parent.Rotation])
        {
            var v = parent.TrueCenter() + vector;
            yield return new Vector3(v.x, AltitudeLayer.MoteOverhead.AltitudeFor(), v.z);
        }
    }
}

public class CompProperties_Effecter : CompProperties
{
    public PositionOffSets positionOffSets;
    public EffecterDef effecterDef;

    public CompProperties_Effecter()
    {
        this.compClass = typeof(Comp_Effecter);
    }
}