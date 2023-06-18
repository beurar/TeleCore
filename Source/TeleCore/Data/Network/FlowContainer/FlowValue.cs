
namespace TeleCore.Network;

public struct FlowValue 
{
    public FlowValueDef Def { get; set; }
    public int Value { get; set; }
    
    public static implicit operator FlowValueDef(FlowValue value) => value.Def;
    public static explicit operator int(FlowValue value) => value.Value;
    
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
    
    public static FlowValue operator /(FlowValue a, int b)
    {
        return new FlowValue(a.Def, a.Value / b);
    }
}