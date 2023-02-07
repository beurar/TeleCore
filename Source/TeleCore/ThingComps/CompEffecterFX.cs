using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TeleCore;

public class CompEffecterFX : ThingComp
{
    private bool isActive;
    private List<EffecterLayer> effecters;

    //
    public CompProperties_Effecter Props => (CompProperties_Effecter) props;
    public CompFX CompFX { get; private set; }
    
    //States
    //TODO: Extend Allow check to affect multiple "thrower layers"

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        CompFX = parent.GetComp<CompFX>();
        if (Props.effecterDef is EffecterExtendedDef fxDef)
        {
            effecter = fxDef.SpawnWithFX(CompFX);
        }
        else
        {
            effecter = Props.effecterDef?.Spawn()!;
        }

        isActive = effecter != null;
    }

    public override void CompTick()
    {
        InternalTick();
    }

    public override void CompTickRare()
    {
        InternalTick();
    }

    private bool CanThrowEffects(int layer)
    {
        
    }
    
    private void InternalTick()
    {
        if (!(isActive && CanThrowEffects())) return;

        foreach (var effecter in effecters)
        {
            if(CompFX?.)
        }
        
        if (!parent.Spawned)
        {
            effecter?.Cleanup();
            effecter = null;
            return;
        }
        
        effecter.EffectTick(parent, parent);
    }
}

public class CompProperties_EffecterFX : CompProperties
{
    public List<EffecterLayerData> effectLayers;

    public CompProperties_EffecterFX()
    {
        compClass = typeof(CompEffecterFX);
    }
}