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

/*public struct DefValueGeneric<TDef, TValue> 
where TDef : Def
where TValue : struct
{
public TDef Def { get; private set; }
public TValue Value { get; set; }
    
public static explicit operator DefValueGeneric<TDef,TValue>(DefValueLoadable<TDef,TValue> defValue)
{
    return new DefValueGeneric<TDef,TValue>(defValue);
}
    
public static implicit operator DefValueGeneric<TDef, TValue>((TDef Def, TValue Value) value) => new (value.Def, value.Value);
public static implicit operator TDef(DefValueGeneric<TDef, TValue> value) => value.Def;
public static explicit operator TValue(DefValueGeneric<TDef, TValue> value) => value.Value;

public DefValueGeneric(DefValueLoadable<TDef,TValue> defValue)
{
    this.Def = defValue.Def;
    this.Value = defValue.Value;
}

public DefValueGeneric(TDef def, TValue value)
{
    this.Def = def;
    this.Value = value;
}

//Float
public static DefValueGeneric<TDef, float> operator +(DefValueGeneric<TDef, TValue> a, float b)
{
    var value1 = a.Value switch
    {
        float f1 => f1,
        int i1 => i1,
        _ => 0
    };

    return new DefValueGeneric<TDef, float>(a.Def, value1 + b);
}

public static DefValueGeneric<TDef, float> operator -(DefValueGeneric<TDef, TValue> a, float b)
{
    var value1 = a.Value switch
    {
        float f1 => f1,
        int i1 => i1,
        _ => 0
    };

    return new DefValueGeneric<TDef, float>(a.Def, value1 - b);
}

public static DefValueGeneric<TDef,float> operator +(DefValueGeneric<TDef,TValue> a, DefValueGeneric<TDef,float> b)
{
    return new DefValueGeneric<TDef, float>
    {
        Def = a.Def ?? b.Def,
        Value = (a + b.Value).Value
    };
}
    
public static DefValueGeneric<TDef, float> operator -(DefValueGeneric<TDef, TValue> a, DefValueGeneric<TDef, float> b)
{
    return new DefValueGeneric<TDef, float>
    {
        Def = a.Def ?? b.Def,
        Value = (a - b.Value).Value
    };
}

//Integer
public static DefValueGeneric<TDef, int> operator +(DefValueGeneric<TDef, TValue> a, int b)
{
    var value1 = a.Value switch
    {
        float f1 => Mathf.RoundToInt(f1),
        int i1 => i1,
        _ => 0
    };

    return new DefValueGeneric<TDef, int>(a.Def, value1 + b);
}

public static DefValueGeneric<TDef, int> operator -(DefValueGeneric<TDef, TValue> a, int b)
{
    return a + (-b);
}

public static DefValueGeneric<TDef, int> operator +(DefValueGeneric<TDef, TValue> a, DefValueGeneric<TDef, int> b)
{
    return a + b.Value;
}

public static DefValueGeneric<TDef, int> operator -(DefValueGeneric<TDef, TValue> a, DefValueGeneric<TDef, int> b)
{
    return a - b.Value;
}
}*/