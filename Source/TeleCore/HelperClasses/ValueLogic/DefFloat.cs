using System.Runtime.InteropServices;
using System.Xml;
using Verse;

namespace TeleCore;

[StructLayout(LayoutKind.Sequential, Size = 6)]
public unsafe struct DefFloat<TDef>
    where TDef : Def
{
    private ushort defID;
    private float value;

    public static implicit operator DefFloat<TDef>((TDef Def, float Value) value) => new (value.Def, value.Value);
    
    public static implicit operator DefValueGeneric<TDef, float>(DefFloat<TDef> defInt) => new (defInt.Def, defInt.value);
    public static implicit operator TDef(DefFloat<TDef> defInt) => defInt.Def;
    public static explicit operator float(DefFloat<TDef> defInt) => defInt.Value;
    
    public TDef Def
    {
        get => defID.ToDef<TDef>();
        set => defID = value.ToID();
    }

    public float Value
    {
        readonly get => value;
        set => this.value = value;
    }

    public DefFloat(DefFloatRef<TDef> defValue)
    {
        Def = defValue.Def;
        Value = defValue.Value;
    }

    public DefFloat(TDef def, float value)
    {
        Def = def;
        Value = value;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        TLog.Error($"Tried to load DefFloat - use {typeof(DefFloatRef<TDef>)} instead! XML: {xmlRoot.ToRefPath()}");
    }

    public override string ToString()
    {
        return $"(({Def.GetType()}):{Def}, {Value})";
    }
    
    #region Arithmetics

    public static DefFloat<TDef> operator +(DefFloat<TDef> a, float b)
    {
        a.value += b;
        return a;
    }

    public static DefFloat<TDef> operator -(DefFloat<TDef> a, float b)
    {
        a.value -= b;
        return a;
    }

    public static DefFloat<TDef> operator +(DefFloat<TDef> a, DefFloat<TDef> b)
    {
        if (a.defID != b.defID) return a;
        a.value += b.value;
        return a;
    }
        
    public static DefFloat<TDef> operator -(DefFloat<TDef> a, DefFloat<TDef> b)
    {
        if (a.defID != b.defID) return a;
        a.value -= b.value;
        return a;
    }

    #endregion
    
    #region Comparision

    public static bool operator ==(DefFloat<TDef> left, DefFloat<TDef> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DefFloat<TDef> left, DefFloat<TDef> right)
    {
        return !(left == right);
    }

    #endregion
}