using Verse;

namespace TeleCore;

public class EffecterLayer
{
    private readonly Effecter _effecter;
    private EffecterLayerData _data;

    public EffecterLayer(EffecterLayerData data, CompFX fxComp = null)
    {
        this._data = data;
        if (data.effecterDef is EffecterExtendedDef exDef)
        {
            _effecter = exDef.SpawnWithFX(fxComp);
        }
        else
        {
            _effecter = data.effecterDef.Spawn();
        }
    }
    
    public void Tick(TargetInfo A, TargetInfo B)
    {
        _effecter.EffectTick(A, B);
    }
}