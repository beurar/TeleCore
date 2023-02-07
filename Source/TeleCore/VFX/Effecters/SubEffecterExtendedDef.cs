using Verse;

namespace TeleCore;

public enum EffectThrowMode
{
    Burst,
    ChancePerTick,
    Continuous
}

public class SubEffecterExtendedDef : SubEffecterDef
{
    public EffectThrowMode effectMode = EffectThrowMode.Continuous;
    public PositionOffsets originOffsets;
    public IntRange burstInterval = new IntRange(0, 0);
    public IntRange throwInterval = new IntRange(40, 100);
    public IntRange soundInterval = new IntRange(40, 100);
    public bool affectedByWind;

    public string eventTag;

    public override string ToString()
    {
        return base.ToString();
    }
}