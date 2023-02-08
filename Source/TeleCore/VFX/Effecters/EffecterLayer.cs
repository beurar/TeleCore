using Verse;

namespace TeleCore;

public class EffecterLayer
{
    private readonly Effecter effecter;

    public EffecterLayer(EffecterDef def, CompFX fxComp = null)
    {
        effecter = def is EffecterExtendedDef exDef ? exDef.SpawnWithFX(fxComp) : def.Spawn();
    }
    
    public void Tick(TargetInfo A, TargetInfo B)
    {
        effecter.EffectTick(A, B);
    }
}