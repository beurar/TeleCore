using System;

namespace TeleCore.Network.PressureSystem.Clamping;

public abstract class ClampWorker
{
    public abstract string Description { get;}

    public abstract ClampConfig Config { get;}
    
    public abstract double ClampFunction(FlowBox t0, FlowBox t1, double f, ClampType type);
    
    public double ClampFlow(double content, double flow, double limit)
    {
        // 'content' can be available fluid or remaining space
        if (content <= 0)
        {
            return 0;
        }

        if (flow >= 0)
        {
            return flow <= limit ? flow : limit;
        }
        return flow >= -limit ? flow : -limit;
    }
}