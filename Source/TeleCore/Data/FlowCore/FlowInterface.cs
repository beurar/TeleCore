using UnityEngine;

namespace TeleCore.FlowCore;

public enum InterfaceFlowMode
{
    FromTo,
    ToFrom, 
    BiDirectional
}

public class FlowInterface<TVolume, TValueDef>
where TValueDef : FlowValueDef
where TVolume : FlowVolume<TValueDef>
{
    public double NextFlow { get; set; } = 0;
    public double PrevFlow { get; set; } = 0;
    public double Move { get; set; } = 0;
    
    public double FlowRate { get; set; }

    public TVolume From { get; private set; }
    public TVolume To { get; private set; }
    public InterfaceFlowMode Mode { get; private set; }

    public float PassPercent { get; private set; } = 1f;
    
    public FlowInterface(TVolume from, TVolume to)
    {
        From = from;
        To = to;
        Mode = InterfaceFlowMode.BiDirectional;
    }

    public FlowInterface(TVolume from, TVolume to, InterfaceFlowMode mode)
    {
        From = from;
        To = to;
        Mode = mode;
    }

    public void UpdateBasedOnFlow(double flow)
    {
        if (flow < 0)
        {
            (From, To) = (To, From);
        }
    }

    public bool ShouldFlow(TVolume from, TVolume to)
    {
        return Mode switch
        {
            InterfaceFlowMode.BiDirectional => true,
            InterfaceFlowMode.FromTo => from == From && to == To,
            InterfaceFlowMode.ToFrom => from == To && to == From,
            _ => false
        };
    }
    
    public TVolume Opposite(TVolume volume)
    {
        return From == volume ? To : From;
    }

    public void SetPassThrough(float percent)
    {
        if (percent is > 1f or < 0f)
        {
            TLog.Warning("Trying to set pass through percent to " + percent + " which is not in range [0, 1]");
            percent = Mathf.Ceil(percent);
        }
        PassPercent = percent;
    }
}