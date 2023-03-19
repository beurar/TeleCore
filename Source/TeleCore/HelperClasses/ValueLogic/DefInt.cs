using System.Runtime.InteropServices;
using System.Xml;
using Verse;

namespace TeleCore;

[StructLayout(LayoutKind.Sequential, Size = 6)]
public struct DefInt<TDef>
    where TDef : Def
{
    private ushort defID;
    private int value;

    public static implicit operator DefInt<TDef>((TDef Def, int Value) value) => new (value.Def, value.Value);

    public static implicit operator DefValueGeneric<TDef, int>(DefInt<TDef> defInt) => new (defInt.Def, defInt.value);
    public static explicit operator DefInt<TDef>(DefValueGeneric<TDef, int> defInt) => new (defInt.Def, defInt.Value);
    public static implicit operator TDef(DefInt<TDef> defInt) => defInt.Def;
    public static explicit operator int(DefInt<TDef> defInt) => defInt.Value;
    
    public TDef Def
    {
        get => defID.ToDef<TDef>();
        set => defID = value.ToID();
    }

    public int Value
    {
        readonly get => value;
        set => this.value = value;
    }

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
        left.value += right;
        return left;
    }

    public static DefInt<TDef> operator -(DefInt<TDef> left, int right)
    {
        left.value -= right;
        return left;
    }

    public static DefInt<TDef> operator +(DefInt<TDef> left, DefInt<TDef> right)
    {
        if (left.defID != right.defID) return left;
        left.value += right.value;
        return left;
    }
        
    public static DefInt<TDef> operator -(DefInt<TDef> left, DefInt<TDef> right)
    {
        if (left.defID != right.defID) return left;
        left.value -= right.value;
        return left;
    }

    #endregion
}