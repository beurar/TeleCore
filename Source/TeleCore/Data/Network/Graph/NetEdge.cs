using System.Diagnostics;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore.Network.Graph;

public interface IEdge<T>
{
    public T From { get; set; }
    public T To { get; set; }
}

[DebuggerDisplay("From: {From} To: {To}")]
public struct NetEdge : IEdge<NetworkPart>
{
    #region Properties
    
    public NetworkPart From { get; set; }
    public NetworkPart To { get; set; }
    
    public IntVec3 FromPos { get; set; }
    public IntVec3 ToPos { get; set; }

    public NetworkIOMode FromIO { get; set; }
    public NetworkIOMode ToIO { get; set; }

    public int Length { get; set; }
    
    #endregion

    public bool BiDirectional => FromIO == NetworkIOMode.TwoWay && ToIO == NetworkIOMode.TwoWay;
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
        From = (NetworkPart) from;
        To = (NetworkPart) to;
        FromPos = fromPos;
        ToPos = toPos;
        FromIO = fromMode;
        ToIO = toMode;
        Length = length;
    }
}