using System;
using TeleCore.Defs;
using TeleCore.Primitive;

namespace TeleCore.Network.Flow.Values;

public struct FlowValue : IComparable<FlowValue>
{
    public FlowValueDef Def { get; set; }
    public double Value { get; set; }
    
    public static implicit operator FlowValueDef(FlowValue value) => value.Def;
    public static explicit operator double(FlowValue value) => value.Value;
    
    public FlowValue(FlowValueDef def, double value)
    {
        Def = def;
        Value = value;
    }

    public static FlowValue operator +(FlowValue a, FlowValue b)
    {
        return new FlowValue(a.Def, a.Value + b.Value);
    }
    
    public static FlowValue operator -(FlowValue a, FlowValue b)
    {
        return new FlowValue(a.Def, a.Value - b.Value);
    }

    public static FlowValue operator *(FlowValue a, FlowValue b)
    {
        return new FlowValue(a.Def, a.Value * b.Value);
    }
    
    public static FlowValue operator /(FlowValue a, FlowValue b)
    {
        return new FlowValue(a.Def, a.Value / b.Value);
    }
    
    public static FlowValue operator +(FlowValue a, double b)
    {
        return new FlowValue(a.Def, a.Value * b);
    }
    
    public static FlowValue operator -(FlowValue a, double b)
    {
        return new FlowValue(a.Def, a.Value / b);
    }
    
    public static FlowValue operator *(FlowValue a, double b)
    {
        return new FlowValue(a.Def, a.Value * b);
    }
    
    public static FlowValue operator /(FlowValue a, double b)
    {
        return new FlowValue(a.Def, a.Value / b);
    }

    public override bool Equals(object obj)
    {
        return (obj is FlowValue other && other.Def == Def && other.Value.Equals(Value));
    }

    public int CompareTo(FlowValue other)
    {
        return Value.CompareTo(other.Value);
    }
}