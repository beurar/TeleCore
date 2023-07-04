namespace TeleCore.Network.Flow;

/// <summary>
///     A connection point for pipes between flow boxes.
/// </summary>
public class FlowInterface
{
    public double Move { get; set; }
    public double Flow { get; set; }
    public double PrevFlow { get; set; }

    public NetworkVolume From { get; }
    public NetworkVolume To { get; }

    public bool Dirty => !(ResolvedMove && ResolvedFlow);
    public bool ResolvedFlow { get; private set; }
    public bool ResolvedMove { get; private set; }
    
    public FlowInterface(NetworkVolume from, NetworkVolume to)
    {
        From = from;
        To = to;
    }

    internal void Notify_SetDirty()
    {
        ResolvedMove = false;
        ResolvedFlow = false;
    }

    internal void Notify_ResolvedMove()
    {
        ResolvedMove = true;
    }

    internal void Notify_ResolvedFlow()
    {
        ResolvedFlow = true;
    }
}