using TeleCore.Network.Data;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore.Network.Graph;

public struct NetEdge
{
    #region Properties

    public NetworkPart From { get; set; }
    public NetworkPart To { get; set; }

    public IntVec3 FromPos { get; set; }
    public IntVec3 ToPos { get; set; }

    public NetworkIOMode FromIO { get; set; }
    public NetworkIOMode ToIO { get; set; }

    #endregion

    public int Length { get; set; }

    public bool BiDirectional { get; set; }
    public bool IsValid => From != null && To != null;

    public NetEdge Reverse => new(To, From, ToPos, FromPos, FromIO, ToIO, Length);

    public static implicit operator NetEdge((NetworkPart, NetworkPart) edge)
    {
        return new NetEdge(edge.Item1, edge.Item2);
    }

    public static implicit operator (NetworkPart, NetworkPart)(NetEdge edge)
    {
        return (edge.From, edge.To);
    }

    public NetEdge(NetworkPart from, NetworkPart to)
    {
        From = from;
        To = to;
    }

    public NetEdge(INetworkPart from, INetworkPart to, IntVec3 fromPos, IntVec3 toPos, NetworkIOMode fromMode,
        NetworkIOMode toMode, int length)
    {
    }
}