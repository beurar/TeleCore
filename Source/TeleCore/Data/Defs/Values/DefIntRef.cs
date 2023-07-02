using TeleCore.Primitive;
using Verse;

namespace TeleCore;

/// <summary>
///     Used to load a <see cref="DefInt{TDef}" /> struct via xml.
/// </summary
public class DefIntRef<TDef> : DefValueLoadable<TDef, int>
    where TDef : Def
{
    //public static explicit operator DefIntRef<TDef>(DefInt<TDef> d) => new(d.Def, d.Value);


    public DefIntRef()
    {
    }


    public DefIntRef(TDef def, int value) : base(def, value)
    {
    }

    public static implicit operator DefInt<TDef>(DefIntRef<TDef> d)
    {
        return new DefInt<TDef>(d.Def, d.Value);
    }
}