using TeleCore.Network.Flow;

namespace TeleCore.Network.Flow.Pressure;

public abstract class PressureWorker
{
    public abstract string Description { get; }
    
    public abstract double CSquared { get; }
    public abstract double Friction { get; }

    public abstract double FlowFunction(FlowBox t0, FlowBox t1, double f);

    public abstract double PressureFunction(FlowBox t);
}