using System.Collections.Generic;
using TeleCore.Network.IO;
using TeleCore.Network.PressureSystem;

namespace TeleCore.Network.Graph;

public struct NetNode
{
    private INetworkSubPart _holder;
    private List<NetInterface> _interfaces;

    public INetworkSubPart Holder => _holder;
    public List<NetInterface> Interfaces => _interfaces;
    
    public NetNode(INetworkSubPart node)
    {
        _holder = node;
    }
}

public struct NetInterface
{
    public INetworkSubPart Holder;
    public INetworkSubPart Endpoint;
    public NetEdge Edge;
}

public struct NetEdge
{
    public NetNode nodeA;
    public NetNode nodeB;
    public IntVec3Rot cellA;
    public IntVec3Rot cellB;
    public NetworkIOMode modeA;
    public NetworkIOMode modeB;
    
}
