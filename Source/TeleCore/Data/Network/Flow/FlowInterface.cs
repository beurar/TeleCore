namespace TeleCore.Network.Flow;

/// <summary>
/// A connection point for pipes between flow boxes.
/// </summary>
public class FlowInterface
{
    private readonly FlowBox _from;
    private readonly FlowBox _to;
    private bool _resolvedFlow, _resolvedMove;

    public double Move { get; set; }
    public double Flow { get; set; }
    public double PrevFlow { get; set; }

    public FlowBox From => _from;
    public FlowBox To => _to;
    
    public bool Dirty => !(_resolvedMove && _resolvedFlow);
    public bool ResolvedFlow => _resolvedFlow;
    public bool ResolvedMove => _resolvedMove;

    public FlowInterface(FlowBox from, FlowBox to)
    {
        _from = from;
        _to = to;
    }

    internal void Notify_SetDirty()
    {
        _resolvedMove = false;
        _resolvedFlow = false;
    }
    
    internal void Notify_ResolvedMove()
    {
        _resolvedMove = true;
    }
    
    internal void Notify_ResolvedFlow()
    {
        _resolvedFlow = true;
    }

}