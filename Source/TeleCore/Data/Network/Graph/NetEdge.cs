using TeleCore.Network.Graph;
using TeleCore.Network.IO;
using UnityEngine;
using Verse;

namespace TeleCore.Network.Graph;

public struct NetEdge
{
    internal readonly int _weight;

    //Direction
    public readonly NetNode nodeA;
    public readonly NetNode nodeB;
    public readonly IntVec3Rot cellA;
    public readonly IntVec3Rot cellB;
    public readonly NetworkIOMode modeA;
    public readonly NetworkIOMode modeB;

    public bool IsDirect => _weight == 0 && cellA == IntVec3.Invalid && cellB == IntVec3.Invalid;
    public bool IsBiDirectional => modeA == NetworkIOMode.TwoWay && modeB == NetworkIOMode.TwoWay;
    
    public bool IsValid
    {
        get
        {
            if (!NetworkCellIO.MatchesFromTo(modeA, modeB)) return false;
            if (nodeA == nodeB) return false;
            if (!cellA.IntVec.IsValid || !cellB.IntVec.IsValid) return false;
            return true;
        }
    }

    public NetEdge Reverse => new NetEdge(nodeB, nodeA, cellB, cellA, modeB, modeA, _weight);

    public static NetEdge Invalid { get; }

    public NetEdge(INetworkSubPart startNode, INetworkSubPart endNode, IntVec3 fromCell, IntVec3 toCell, NetworkIOMode fromMode, NetworkIOMode toMode, int weight)
    {
        this.nodeA = startNode;
        this.nodeB = endNode;
        this.cellA = fromCell;
        this.cellB = toCell;
        this.modeA = fromMode;
        this.modeB = toMode;
        this._weight = weight;
    }

    public NetEdge(INetworkSubPart startNode, INetworkSubPart endNode)
    {
        this.nodeA = startNode;
        this.nodeB = endNode;
        this.cellA = IntVec3.Invalid;
        this.cellB = IntVec3.Invalid;
        this.modeA = NetworkIOMode.None;
        this.modeB = NetworkIOMode.None;
        this._weight = 0;
    }

    public bool HasAnchorCell(IntVec3 cell)
    {
        return cellA == cell || cellB == cell;
    }

    public string ToStringSimple(INetworkSubPart node)
    {
        return $"{($"{nodeA.Holder.Parent.Thing}".Colorize(node == nodeA.Holder ? Color.cyan : Color.white))} -> {($"{nodeB.Holder.Parent.Thing}".Colorize(node == nodeB.Holder ? Color.cyan : Color.white))}";
    }

    public string ToString(INetworkSubPart node)
    {
        return $"{($"{nodeA.Holder.Parent.Thing}".Colorize(node == nodeA.Holder ? Color.cyan : Color.white))} -> {($"{nodeB.Holder.Parent.Thing}".Colorize(node == nodeB.Holder ? Color.cyan : Color.white))}| ({cellA},{cellB})";
    }
}