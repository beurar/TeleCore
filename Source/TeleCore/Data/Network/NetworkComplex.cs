using TeleCore.Network.PressureSystem;

namespace TeleCore.Network;

/// <summary>
/// Organizes and manages the bundle of components to define a single Network.
/// </summary>
public class NetworkComplex
{
    private PipeNetwork network; //Handles network management logic
    private NetGraph graph; //Handles all connection and relation logic
    private FlowSystem flow; //Handles flow logic

    public PipeNetwork Network => network;
    public NetGraph Graph => graph;
    public FlowSystem FlowSystem => flow;
    
    public static NetworkComplex Create(INetworkSubPart root, PipeNetworkSystem system, out PipeNetwork newNet,
        out NetGraph newGraph, out FlowSystem newFlowSys)
    {
        var complex = new NetworkComplex();
        newNet = new PipeNetwork(root.NetworkDef, system);
        newGraph = new NetGraph();
        newFlowSys = new FlowSystem();
        
        //
        newNet.Graph = newGraph;

        return complex;
    }

    public void Tick()
    {
        network.Tick();
        flow.Tick();
    }
}