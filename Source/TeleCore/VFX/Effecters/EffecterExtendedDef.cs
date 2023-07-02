using Verse;

namespace TeleCore;

public class EffecterExtendedDef : EffecterDef
{
    public Effecter_FX SpawnWithFX(CompFX fxComp)
    {
        return new Effecter_FX(fxComp, this);
    }
}