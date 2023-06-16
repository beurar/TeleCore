namespace TeleCore.Network.PressureSystem.Pressure;

public abstract class PressureWorker
{
    public abstract string Description { get; }

    public abstract PressureConfig Config { get; }
    
    public abstract float FlowFunction(FlowBox t0, FlowBox t1, float f);

    public abstract float PressureFunction(FlowBox t);
}