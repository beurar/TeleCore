using System.Runtime.InteropServices;
using System.Xml;
using Verse;

namespace TeleCore;

[StructLayout(LayoutKind.Sequential, Size = 6)]
public struct DefFloat<TDef> : IExposable
    where TDef : Def
{
    private ushort defID;
    private float value;

    public static implicit operator DefFloat<TDef>((TDef Def, float Value) value) => new (value.Def, value.Value);
    
    public static implicit operator DefValueLoadable<TDef, float>(DefFloat<TDef> defInt) => new (defInt.Def, defInt.value);
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
        //TLog.Debug($"Making {this} with {defValue.def}|{defValue.value} -> {defValue.def.ToID()} | {defValue.def.index}");
        Def = defValue.Def;
        Value = defValue.Value;
    }

    public DefFloat(TDef def, float value)
    {
        //TLog.Debug($"Making {this} with {def}|{value} -> {def.ToID()} | {def.index}");
        Def = def;
        Value = value;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        TLog.Error($"Tried to load DefFloat - use 'DefFloatRef' instead! XML: {xmlRoot.ToRefPath()}");
    }

    public override string ToString()
    {
        return $"(({typeof(TDef)}):[{defID}]{Def}, {Value})";
    }

    public void ExposeData()
    {
        DefFloatRef<TDef> defRef = (DefFloatRef<TDef>) this;
        Scribe_Deep.Look(ref defRef, "defRef");

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            this.Def = defRef.Def;
            this.Value = defRef.Value;
        }
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
    
    public static DefFloat<TDef> operator *(DefFloat<TDef> a, float b)
    {
        a.value *= b;
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