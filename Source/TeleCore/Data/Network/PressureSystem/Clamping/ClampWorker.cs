using System;

namespace TeleCore.Network.PressureSystem.Clamping;

public abstract class ClampWorker
{
    public abstract string Description { get;}

    public abstract ClampConfig Config { get;}
    
    public abstract float ClampFunction(FlowBox t0, FlowBox t1, float f, ClampType type);
    
    public float ClampFlow(float content, float flow, float limit)
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