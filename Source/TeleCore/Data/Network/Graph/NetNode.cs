using TeleCore.Network.Data;

namespace TeleCore.Network.Graph;

public struct NetNode
{
    public NetworkPart Value { get; }

    public static implicit operator NetworkPart(NetNode node)
    {
        return node.Value;
    }

    public static implicit operator NetNode(NetworkPart node)
    {
        return new NetNode(node);
    }

    public NetNode(NetworkPart value)
    {
        Value = value;
    }
}