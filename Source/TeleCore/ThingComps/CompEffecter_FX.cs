using RimWorld;
using Verse;

namespace TeleCore;

public class CompEffecter_FX : CompEffecter
{
    private bool isActive;

    //
    public CompProperties_Effecter Props => (CompProperties_Effecter) props;
    public IFXHolder IParent => (IFXHolder) parent;
    

    //States
    //TODO: Extend Allow check to affect multiple "thrower layers"
    public bool CanThrowEffects => IParent == null || IParent.ShouldDoEffects;

    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        effecter = Props.effecterDef?.Spawn()!;
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

    private void InternalTick()
    {
        if (!(isActive && CanThrowEffects)) return;
        
        if (!parent.Spawned)
        {
            effecter?.Cleanup();
            effecter = null;
            return;
        }
        
        effecter.EffectTick(parent, parent);
    }
}