using System;

namespace TeleCore.Network.Flow.Clamping;

public class ClampWorker_ConnectionCountLimit: ClampWorker
{
    private FlowSystem _parentSystem;
    
    public override string Description => "Limit flow to (1/connections) of current content (outflow) or remaining space (inflow)";

    public override bool EnforceMinPipe => true;
    public override bool EnforceMaxPipe => true;
    public override bool MaintainFlowSpeed => false;
    public override double MinDivider => 1;
    public override double MaxDivider => 1;

    public ClampWorker_ConnectionCountLimit(FlowSystem parentSystem)
    {
        _parentSystem = parentSystem;
    }
    
    public override double ClampFunction(FlowBox t0, FlowBox t1, double f, ClampType type)
    {
        double d0 = 1d / Math.Max(1, _parentSystem.ConnectionTable[t0].Count);
        double d1 = 1d / Math.Max(1, _parentSystem.ConnectionTable[t1].Count);
        double c, r;
        if (EnforceMinPipe)
        {
            if (f > 0)
            {
                c = t0.TotalValue;
                f = ClampFlow(c, f, d0 * c);
            }
            else if (f < 0)
            {
                c = t1.TotalValue;
                f = -ClampFlow(c, -f, d1 * c);
            }
        }
        if (EnforceMaxPipe)
        {
            if (f > 0)
            {
                r = t1.MaxCapacity - t1.TotalValue;
                f = ClampFlow(r, f, d1 * r);
            }
            else if (f < 0)
            {
                r = t0.MaxCapacity - t0.TotalValue;
                f = -ClampFlow(r, -f, d0 * r);
            }
        }
        return f;
    }
}