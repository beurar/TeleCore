using Verse;

namespace TeleCore;

public class EffecterLayer
{
    public Effecter effecter;
    public EffecterLayerData data;

    public void Tick(TargetInfo A, TargetInfo B)
    {
        effecter.EffectTick(A, B);
    }
}