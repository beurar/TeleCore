using System.Collections.Generic;

namespace TeleCore.Network;

/// <summary>
/// Redundant replication 
/// </summary>
public struct FlowValueStack
{
    private FlowValue[] _stack;
    
    public FlowValueStack()
    {
        
    }

    public FlowValueStack(List<FlowValueDef> types, int capacity)
    {
        
    }

    public DefValueStack<FlowValueDef> ToValueStack => new();
}

public struct FlowValue 
{
    public FlowValueDef Def { get; set; }
    public int Value { get; set; }

    public static implicit operator int(FlowValue value) => value.Value;
    public static implicit operator FlowValueDef(FlowValue value) => value.Def;
    
    public FlowValue(FlowValueDef def, int value)
    {
    }

    public DefValue<FlowValueDef> ToDefValue() => new (Def, Value);
    
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
}