namespace TeleCore.Network.PressureSystem.Pressure;

public abstract class PressureWorker
{
    public abstract string Description { get; }

    public abstract PressureConfig Config { get; }
    
    public abstract double FlowFunction(FlowBox t0, FlowBox t1, double f);

    public abstract double PressureFunction(FlowBox t);
}