using Verse;

namespace TeleCore;

/// <summary>
/// Used to load a <see cref="DefFloat{TDef}"/> struct via xml.
/// </summary>
public class DefFloatRef<TDef> : DefValueLoadable<TDef, float> where TDef : Def
{
    public static implicit operator DefFloat<TDef>(DefFloatRef<TDef> d) => new DefFloat<TDef>(d.Def, d.Value);
    public static explicit operator DefFloatRef<TDef>(DefFloat<TDef> d) => new (d.Def, d.Value);

    public DefFloatRef() {}

    public DefFloatRef(TDef def, float value) : base(def, value)
    {
    }
        
    public string ToStringPercent()
    {
        return $"{Def?.defName}: ({Value.ToStringPercent()})";
    }
}