using UnityEngine;
using Verse;

namespace TeleCore;

public struct NetEdge
{
    internal readonly int _weight;

    //Direction
    public readonly INetworkSubPart fromNode;
    public readonly INetworkSubPart toNode;
    public readonly IntVec3Rot fromCell;
    public readonly IntVec3Rot toCell;

    public NetEdge Reverse => new(toNode, fromNode, toCell, fromCell, _weight);
    public static NetEdge Invalid => new(null, null, IntVec3.Invalid, IntVec3.Invalid, -1);

    public bool IsDirect => _weight == 0 && fromCell == IntVec3.Invalid && toCell == IntVec3.Invalid;

    public NetEdge(INetworkSubPart fromNode, INetworkSubPart toNode, IntVec3 fromCell, IntVec3 toCell, int weight)
    {
        this.fromNode = fromNode;
        this.toNode = toNode;
        this.fromCell = fromCell;
        this.toCell = toCell;
        this._weight = weight;
    }

    public NetEdge(INetworkSubPart fromNode, INetworkSubPart toNode)
    {
        this.fromNode = fromNode;
        this.toNode = toNode;
        this.fromCell = IntVec3.Invalid;
        this.toCell = IntVec3.Invalid;
        this._weight = 0;
    }

    public bool HasAnchorCell(IntVec3 cell)
    {
        return fromCell == cell || toCell == cell;
    }

    public string ToStringSimple(INetworkSubPart node)
    {
        return $"{($"{fromNode.Parent.Thing}".Colorize(node == fromNode ? Color.cyan : Color.white))} -> {($"{toNode.Parent.Thing}".Colorize(node == toNode ? Color.cyan : Color.white))}";
    }

    public string ToString(INetworkSubPart node)
    {
        return $"{($"{fromNode.Parent.Thing}".Colorize(node == fromNode ? Color.cyan : Color.white))} -> {($"{toNode.Parent.Thing}".Colorize(node == toNode ? Color.cyan : Color.white))}| ({fromCell},{toCell})";
    }
}