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

    public static implicit operator NetNode(NetworkSubPart subPart) => new NetNode(subPart);
    public static implicit operator NetworkSubPart(NetNode node) => node._holder as NetworkSubPart;
    
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
