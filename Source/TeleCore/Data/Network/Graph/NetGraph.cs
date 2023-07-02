using System;
using System.Collections.Generic;
using TeleCore.Network.Data;
using Verse;

namespace TeleCore.Network.Graph;

public class NetGraph : IDisposable
{
    public NetGraph()
    {
        Nodes = new List<NetNode>();
        Edges = new List<NetEdge>();
        AdjacencyList = new Dictionary<NetNode, List<(NetEdge, NetNode)>>();
        EdgeLookUp = new Dictionary<(NetNode, NetNode), NetEdge>();
    }

    public List<NetNode> Nodes { get; private set; }

    public List<NetEdge> Edges { get; private set; }

    public Dictionary<NetNode, List<(NetEdge, NetNode)>> AdjacencyList { get; private set; }

    public Dictionary<(NetNode, NetNode), NetEdge> EdgeLookUp { get; private set; }

    public List<IntVec3> Cells { get; private set; }

    public void Dispose()
    {
        Cells.Clear();
        Nodes.Clear();
        Edges.Clear();
        AdjacencyList.Clear();
        EdgeLookUp.Clear();

        Cells = null;
        Nodes = null;
        Edges = null;
        AdjacencyList = null;
        EdgeLookUp = null;
    }

    public List<(NetEdge, NetNode)>? GetAdjacencyList(INetworkPart forPart)
    {
        if (forPart is NetworkPart part)
            if (AdjacencyList.TryGetValue(part, out var list))
                return list;

        return null;
    }

    private void AddNode(NetNode node, NetEdge fromEdge)
    {
        Nodes.Add(node);
        AdjacencyList.Add(node, new List<(NetEdge, NetNode)>());
    }

    internal void AddCells(INetworkPart netPart)
    {
        foreach (var cell in netPart.Thing.OccupiedRect()) Cells.Add(cell);
    }

    internal bool AddEdge(NetEdge edge)
    {
        //Ignore invalid edges
        if (!edge.IsValid) return true;

        //Check existing
        var key = (fromNode: (NetNode) edge.From, toNode: (NetNode) edge.To);
        if (EdgeLookUp.ContainsKey(key))
        {
            TLog.Warning($"Key ({edge.From}, {edge.To}) already exists in graph!");
            return false;
        }

        EdgeLookUp.Add(key, edge);
        if (!AdjacencyList.TryGetValue(edge.From, out var listSource)) //Check if node exists to have any adj nghbrs
        {
            AddNode(edge.From, edge); //Add starting node as known node
            listSource = AdjacencyList[edge.From];
        }

        if (!listSource.Contains((edge,
                edge.To))) //Check if edge is already int adjancy list for starting node (edge.From)
        {
            listSource.Add((edge, edge.To));
            AdjacencyList[edge.From].Add((edge, edge.To));
        }

        return true;
    }

    internal void Draw()
    {
        throw new NotImplementedException();
    }

    internal void OnGUI()
    {
        throw new NotImplementedException();
    }
}