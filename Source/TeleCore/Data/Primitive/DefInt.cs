using System.Runtime.InteropServices;
using System.Xml;
using Verse;

namespace TeleCore.Primitive;

[StructLayout(LayoutKind.Sequential, Size = 6)]
public struct DefInt<TDef> where TDef : Def
{
    private ushort defID;

    public static implicit operator DefInt<TDef>((TDef Def, int Value) value)
    {
        return new DefInt<TDef>(value.Def, value.Value);
    }

    public static implicit operator DefValueLoadable<TDef, int>(DefInt<TDef> defInt)
    {
        return new DefValueLoadable<TDef, int>(defInt.Def, defInt.Value);
    }

    public static explicit operator DefInt<TDef>(DefValueLoadable<TDef, int> defInt)
    {
        return new DefInt<TDef>(defInt.Def, defInt.Value);
    }

    public static implicit operator TDef(DefInt<TDef> defInt)
    {
        return defInt.Def;
    }

    public static explicit operator int(DefInt<TDef> defInt)
    {
        return defInt.Value;
    }

    public TDef Def
    {
        get => defID.ToDef<TDef>();
        set => defID = value.ToID();
    }

    public int Value { get; set; }

    public DefInt(DefIntRef<TDef> defValue)
    {
        Def = defValue.Def;
        Value = defValue.Value;
    }

    public DefInt(TDef def, int value)
    {
        Def = def;
        Value = value;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        TLog.Error($"Tried to load DefInt struct - use 'DefIntRef' instead! XML: {xmlRoot.ToRefPath()}");
    }

    public override string ToString()
    {
        return $"(({Def.GetType()}):{Def}, {Value})";
    }

    #region Arithmetics

    public static DefInt<TDef> operator +(DefInt<TDef> left, int right)
    {
        left.Value += right;
        return left;
    }

    public static DefInt<TDef> operator -(DefInt<TDef> left, int right)
    {
        left.Value -= right;
        return left;
    }

    public static DefInt<TDef> operator +(DefInt<TDef> left, DefInt<TDef> right)
    {
        if (left.defID != right.defID) return left;
        left.Value += right.Value;
        return left;
    }

    public static DefInt<TDef> operator -(DefInt<TDef> left, DefInt<TDef> right)
    {
        if (left.defID != right.defID) return left;
        left.Value -= right.Value;
        return left;
    }

    #endregion
}