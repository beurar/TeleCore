using TeleCore.Network.Data;

namespace TeleCore.Network.Graph;

public struct NetNode
{
    public NetworkPart Value { get; }

    public static implicit operator NetworkPart(NetNode node) => node.Value;
    public static implicit operator NetNode(NetworkPart node) => new NetNode(node);

    public NetNode(NetworkPart value)
    {
        Value = value;
    }
}