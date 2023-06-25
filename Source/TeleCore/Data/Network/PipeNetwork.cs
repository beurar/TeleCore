using System;
using TeleCore.Defs;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using TeleCore.Network.Utility;

namespace TeleCore.Network;

public class PipeNetwork : IDisposable
{
    private NetworkDef _def;
    private int _id;

    private NetGraph _graph;
    private FlowSystem _flowSystem;
    protected NetworkPartSetExtended _partSet;    
    
    public int ID => _id;
    public NetworkDef NetworkDef => _def;
    
    public NetGraph Graph => _graph;
    public FlowSystem FlowSystem => _flowSystem;
    public NetworkPartSet PartSet => _partSet;
    
    public PipeNetwork(NetworkDef def)
    {
        _id = PipeNetworkFactory.MasterNetworkID++;
        _def = def;
        _partSet = new NetworkPartSetExtended(def);
    }

    public void Dispose()
    {
        _graph.Dispose();
        _flowSystem.Dispose();
        _partSet.Dispose();
    }
    
    internal void PrepareForRegen(out NetGraph graph, out FlowSystem flow)
    {
        graph = _graph = new NetGraph();
        flow = _flowSystem = new FlowSystem();
    }

    internal bool Notify_AddPart(INetworkPart part)
    {
        return _partSet.AddComponent(part);
    }

    //TODO: not needed right now as networks are always fully re-generated
    /*
    internal void Notify_RemovePart(INetworkPart part)
    {
        ParentSystem.Notify_RemovePart(part);

        //
        PartSet.RemoveComponent(part);
        containerSet.RemoveContainerFrom(part);
        foreach (var cell in part.Parent.Thing.OccupiedRect())
        {
            NetworkCells.Remove(cell);
        }
    }
    */
    
    internal void Tick()
    {
        _flowSystem.Tick();
        foreach (var part in _partSet.TickSet)
        {
            part.Tick();
        }
    }

    internal void Draw()
    {
        _flowSystem.Draw();
        _graph.Draw();
    }

    internal void OnGUI()
    {
        _flowSystem.OnGUI();
        _graph.OnGUI();
    }
}