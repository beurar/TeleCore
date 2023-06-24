using TeleCore.Defs;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using TeleCore.Network.Utility;

namespace TeleCore.Network;

public class PipeNetwork
{
    private NetworkDef _def;
    private int _id;

    private NetGraph _graph;
    private FlowSystem _flowSystem;
    
    public int ID => _id;
    public NetworkDef NetworkDef => _def;

    public PipeNetwork(NetworkDef def)
    {
        _id = PipeNetworkFactory.MasterNetworkID++;
        _def = def;
    }

    internal void PrepareForRegen(out NetGraph graph, out FlowSystem flow)
    {
        graph = _graph = new NetGraph();
        flow = _flowSystem = new FlowSystem();
    }

    internal void Tick()
    {
        _flowSystem.Tick();
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