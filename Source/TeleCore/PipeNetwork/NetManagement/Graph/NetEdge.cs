using TeleCore.Data.Network.IO;
using UnityEngine;
using Verse;

namespace TeleCore;

public struct NetEdge
{
    internal readonly int _weight;

    //Direction
    public readonly INetworkSubPart startNode;
    public readonly INetworkSubPart endNode;
    public readonly IntVec3Rot fromCell;
    public readonly IntVec3Rot toCell;
    public readonly NetworkIOMode fromMode;
    public readonly NetworkIOMode toMode;

    //public NetEdge Reverse => new(toNode, fromNode, toCell, fromCell, _weight);
    //public static NetEdge Invalid => new(null, null, IntVec3.Invalid, IntVec3.Invalid, -1);

    public bool IsDirect => _weight == 0 && fromCell == IntVec3.Invalid && toCell == IntVec3.Invalid;
    public bool IsBiDirectional => fromMode == NetworkIOMode.TwoWay && toMode == NetworkIOMode.TwoWay;
    
    public bool IsValid
    {
        get
        {
            if (!NetworkCellIO.MatchesFromTo(fromMode, toMode)) return false;
            if (startNode == endNode) return false;
            if (!fromCell.IntVec.IsValid || !toCell.IntVec.IsValid) return false;
            return true;
        }
    }

    public NetEdge Reverse => new NetEdge(endNode, startNode, toCell, fromCell, toMode, fromMode, _weight);

    public static NetEdge Invalid { get; }

    public NetEdge(INetworkSubPart startNode, INetworkSubPart endNode, IntVec3 fromCell, IntVec3 toCell, NetworkIOMode fromMode, NetworkIOMode toMode, int weight)
    {
        this.startNode = startNode;
        this.endNode = endNode;
        this.fromCell = fromCell;
        this.toCell = toCell;
        this.fromMode = fromMode;
        this.toMode = toMode;
        this._weight = weight;
    }

    public NetEdge(INetworkSubPart startNode, INetworkSubPart endNode)
    {
        this.startNode = startNode;
        this.endNode = endNode;
        this.fromCell = IntVec3.Invalid;
        this.toCell = IntVec3.Invalid;
        this.fromMode = NetworkIOMode.None;
        this.toMode = NetworkIOMode.None;
        this._weight = 0;
    }

    public bool HasAnchorCell(IntVec3 cell)
    {
        return fromCell == cell || toCell == cell;
    }

    public string ToStringSimple(INetworkSubPart node)
    {
        return $"{($"{startNode.Parent.Thing}".Colorize(node == startNode ? Color.cyan : Color.white))} -> {($"{endNode.Parent.Thing}".Colorize(node == endNode ? Color.cyan : Color.white))}";
    }

    public string ToString(INetworkSubPart node)
    {
        return $"{($"{startNode.Parent.Thing}".Colorize(node == startNode ? Color.cyan : Color.white))} -> {($"{endNode.Parent.Thing}".Colorize(node == endNode ? Color.cyan : Color.white))}| ({fromCell},{toCell})";
    }
}