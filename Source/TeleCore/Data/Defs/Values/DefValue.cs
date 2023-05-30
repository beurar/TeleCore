using OneOf;
using UnityEngine;
using Verse;

namespace TeleCore;

/// <summary>
/// Wraps any <see cref="Def"/> Type into a struct, attaching a numeric value
/// </summary>
/// <typeparam name="TDef">The <see cref="Def"/> Type of the value.</typeparam>
public struct DefValue<TDef> where TDef : Def
{
    public TDef Def { get; private set; }
    public OneOf<int, float> Value { get; set; }

    public int ValueInt => Value.AsT0; 
    public float ValueFloat => Value.AsT1; 
        
    public static implicit operator DefValue<TDef>((TDef Def, OneOf<int, float >Value) value) => new (value.Def, value.Value);
    public static implicit operator TDef(DefValue<TDef> defInt) => defInt.Def;
    public static explicit operator OneOf<int, float>(DefValue<TDef> defInt) => defInt.Value;

    public DefValue(DefValueLoadable<TDef> defValue)
    {
        Def = defValue.Def;
        Value = defValue.Value;
    }

    public DefValue(TDef def, OneOf<int, float> value)
    {
        Def = def;
        Value = value;
    }

    public static DefValue<TDef> operator +(DefValue<TDef> a, int b)
    {
        var value = b + a.Value.AsT0;
        return new DefValue<TDef>(a.Def, value);
    }
        
    public static DefValue<TDef> operator +(DefValue<TDef> a, float b)
    {
        var value = b + a.Value.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }

    public static DefValue<TDef> operator +(DefValue<TDef> a, DefValue<TDef> b)
    {
        OneOf<int, float> value = 0;
        if (b.Value.IsT0)
            value = a.Value.AsT0 + b.Value.AsT0;
        if (b.Value.IsT1)
            value = a.Value.AsT1 + b.Value.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }

    public static DefValue<TDef> operator -(DefValue<TDef> a, int b)
    {
        var value = b - a.Value.AsT0;
        return new DefValue<TDef>(a.Def, value);
    }
        
    public static DefValue<TDef> operator -(DefValue<TDef> a, float b)
    {
        var value = b - a.Value.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }
    
    public static DefValue<TDef> operator -(DefValue<TDef> a, DefValue<TDef> b)
    {
        OneOf<int, float> value = 0;

        if (b.Value.IsT0)
            value = a.Value.AsT0 - b.Value.AsT0;
        if (b.Value.IsT1)
            value = a.Value.AsT1 - b.Value.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }
    
    public static DefValue<TDef> operator *(DefValue<TDef> a, int b)
    {
        var value = b * a.Value.AsT0;
        return new DefValue<TDef>(a.Def, value);
    }
        
    public static DefValue<TDef> operator *(DefValue<TDef> a, float b)
    {
        var value = b * a.Value.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }
    
    public static DefValue<TDef> operator *(DefValue<TDef> a, DefValue<TDef> b)
    {
        OneOf<int, float> value = 0;
        if (b.Value.IsT0)
            value = a.Value.AsT0 * b.Value.AsT0;
        if (b.Value.IsT1)
            value = a.Value.AsT1 * b.Value.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }
    
    public static DefValue<TDef> operator /(DefValue<TDef> a, int b)
    {
        var value = b / a.Value.AsT0;
        return new DefValue<TDef>(a.Def, value);
    }
        
    public static DefValue<TDef> operator /(DefValue<TDef> a, float b)
    {
        var value = b / a.Value.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }

    public static DefValue<TDef> operator /(DefValue<TDef> a, DefValue<TDef> b)
    {
        return a / b.Value;
    }
    
    public static DefValue<TDef> operator /(DefValue<TDef> a, OneOf<int, float> b)
    {
        OneOf<int, float> value = 0;
        if (b.IsT0)
            value = a.Value.AsT0 / b.AsT0;
        if (b.IsT1)
            value = a.Value.AsT1 / b.AsT1;
        return new DefValue<TDef>(a.Def, value);
    }
    
    public override string ToString()
    {
        return $"(({Def.GetType()}):{Def}, {Value})";
    }
}