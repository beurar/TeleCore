using Verse;

namespace TeleCore.Network.PressureSystem.Pressure;

public class PressureWorker_WaveEquation : PressureWorker
{
    public override string Description => "Wave equation with linear pressure.";
    
    public override PressureConfig Config => new PressureConfig 
    {
        CSquared = new ConfigItem 
        {
            Value = 0.01f,
            Range = new float[] {0, 1},
            Description = "C-Squared"
        },
        Friction = new ConfigItem 
        {
            Value = 0.001f,
            Range = new float[] {0, 1},
            Description = "Friction"
        }
    };
    
    public override float FlowFunction(FlowBox t0, FlowBox t1, float f) 
    {
        var cfg = this.Config;
        f += (this.PressureFunction(t0) - this.PressureFunction(t1)) * cfg.CSquared.Value;
        f *= 1 - cfg.Friction.Value;
        return f;
    }
    
    public override float PressureFunction(FlowBox t) 
    {
        return t.Content / t.MaxContent * 100;
    }
}