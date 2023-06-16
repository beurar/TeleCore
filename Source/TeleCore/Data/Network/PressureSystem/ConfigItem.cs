namespace TeleCore.Network.PressureSystem;

public class ConfigItem 
{
    public float Value { get; set; }
    public float[] Range { get; set; }
    public string Description { get; set; }
    
    public static implicit operator float(ConfigItem option) => option.Value;
}

public class ConfigOption 
{
    public bool Value { get; set; }
    public string Description { get; set; }

    public static implicit operator bool(ConfigOption option) => option.Value;
}