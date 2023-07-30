namespace TeleCore.FlowCore;

public enum InterfaceFlowMode
{
    FromTo,
    ToFrom, 
    BiDirectional
}

public interface IFlowInterface
{
    public double NextFlow { get; set; }
    public double PrevFlow { get; set; }
    public double Move { get; set; }
    public double FlowRate { get; set; }
}

public class MultiFlowInterface<TVolumeOne, TVolumeTwo, TValueDefOne, TValueDefTwo>
    where TValueDefOne : FlowValueDef
    where TVolumeOne : FlowVolume<TValueDefOne>
    where TValueDefTwo : FlowValueDef
    where TVolumeTwo : FlowVolume<TValueDefTwo>
{
    public TVolumeOne From { get; private set; }
    public TVolumeTwo To { get; private set; }
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
    
    public float PassPercent => 1f;
    
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
}