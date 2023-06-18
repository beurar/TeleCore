namespace TeleCore.Network.PressureSystem.Clamping;

public class ClampWorker_Overcommit : ClampWorker
{
    public override string Description => "Limit flow to a configurable fraction of current content (outflow) or remaining space (inflow)";
    
    public override ClampConfig Config => new ClampConfig 
    {
        EnforceMinPipe = new ConfigOption 
        {
            Value = true,
            Description = "Enforce pipe min content (= 0)"
        },
        EnforceMaxPipe = new ConfigOption 
        {
            Value = true,
            Description = "Enforce pipe max content (= 100)"
        },
        MaintainFlowSpeed = new ConfigOption 
        {
            Value = false,
            Description = "Do not reduce flow speed when clamping"
        },
        MinDivider = new ConfigItem 
        {
            Value = 4,
            Range = new float[] {1, 4},        
            Description = "Divider for available fluid [1..4]"
        },
        MaxDivider = new ConfigItem() 
        {
            Value = 1,
            Range = new float[] {1, 4},        
            Description = "Divider for remaining space [1..4]"
        },      
    };
    
    public override double ClampFunction(FlowBox t0, FlowBox t1, double f, ClampType type) 
    {
        var cfg = this.Config;     
        float d, c, r;
        if (cfg.EnforceMinPipe.Value) 
        {
            // Limit outflow to 1/divider of fluid content in src pipe     
            if (type == ClampType.FlowSpeed && cfg.MaintainFlowSpeed) 
            {
                d = 1;
            }
            else 
            {
                d = 1 / cfg.MinDivider.Value;
            }
            if (f > 0) 
            {
                c = t0.Content;
                f = ClampFlow(c, f, d*c);
            } 
            else if (f < 0) 
            {
                c = t1.Content;
                f = -ClampFlow(c, -f, d*c);
            }
        }
        if (cfg.EnforceMaxPipe.Value && (type == ClampType.FluidMove || !cfg.MaintainFlowSpeed.Value)) 
        {
            // Limit inflow to 1/divider of remaining space in dst pipe
            d = 1 / cfg.MaxDivider.Value;
            if (f > 0) 
            {
                r = t1.MaxContent - t1.Content;
                f = ClampFlow(r, f, d*r);
            } 
            else if (f < 0) 
            {
                r = t0.MaxContent - t0.Content;
                f = -ClampFlow(r, -f, d*r);
            }      
        }
        return f;
    }
}