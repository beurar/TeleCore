namespace TeleCore.FlowCore;

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
    public float PassPercent => 1f;

    public FlowInterface(TVolume from, TVolume to)
    {
        From = from;
        To = to;
    }
    
    public void UpdateBasedOnFlow(double flow)
    {
        if (flow < 0)
        {
            (From, To) = (To, From);
        }
    }
    
    public TVolume Opposite(TVolume volume)
    {
        return From == volume ? To : From;
    }
}