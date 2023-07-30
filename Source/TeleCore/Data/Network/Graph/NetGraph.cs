using System;
using System.Collections.Generic;
using System.Diagnostics;
using TeleCore.Network.Data;
using Verse;

namespace TeleCore.Network.Graph;

public class DataGraph<TNode, TEdge> : IDisposable
where TEdge : IEdge<TNode>
{
    public List<TNode> Nodes { get; private set; }

    public List<TEdge> Edges { get; private set; }
    
    public Dictionary<TNode, List<(TEdge, TNode)>> AdjacencyList { get; private set; }

    public Dictionary<(TNode, TNode), TEdge> EdgeLookUp { get; private set; }
    
    public void Dispose()
    {
        // TODO release managed resources here
    }
}

[DebuggerDisplay("{Nodes.Count} | {Edges.Count}")]
public class NetGraph : IDisposable
{
    public NetGraph()
    {
        Nodes = new List<NetNode>();
        Edges = new List<NetEdge>();
        AdjacencyList = new Dictionary<NetNode, List<(NetEdge, NetNode)>>();
        EdgeLookUp = new Dictionary<(NetNode, NetNode), NetEdge>();
        Cells = new List<IntVec3>();
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
        {
            if (AdjacencyList.TryGetValue(part, out var list))
                return list;
        }

        return null;
    }
    internal void AddCells(INetworkPart netPart)
    {
        foreach (var cell in netPart.Thing.OccupiedRect())
        {
            Cells.Add(cell);
        }
    }

    internal bool AddEdge(NetEdge edge)
    {
        //Ignore invalid edges
        if (!edge.IsValid) return false;

        //Check existing
        var key = (fromNode: (NetNode) edge.From, toNode: (NetNode) edge.To);
        if (EdgeLookUp.ContainsKey(key))
        {
            TLog.Warning($"Key ({edge.From}, {edge.To}) already exists in graph!");
            return false;
        }

        Edges.Add(edge);
        EdgeLookUp.Add(key, edge);

        //One Directional Edges have custom logic
        if (!edge.BiDirectional)
        {
            Nodes.Add(edge.From);
            Nodes.Add(edge.To);
            if (!AdjacencyList.TryGetValue(edge.From, out var listSource))
            {
                listSource = new List<(NetEdge, NetNode)>()
                {
                    (edge, edge.To)
                };
                AdjacencyList.Add(edge.From, listSource);
            }
        }
        else
        {
            Nodes.Add(edge.From);
            
            //Check if node exists to have any adj neighbors
            if (!AdjacencyList.TryGetValue(edge.From, out var listSource))
            {
                listSource = new List<(NetEdge, NetNode)>()
                {
                    (edge, edge.To)
                };
                AdjacencyList.Add(edge.From, listSource);
            }

            //Check if edge is already int adjancy list for starting node (edge.From)
            // if (!listSource.Contains((edge, edge.To)))
            // {
            //     listSource.Add((edge, edge.To));
            //     //AdjacencyList[edge.From].Add((edge, edge.To));
            // }
        }
        return true;
    }
    
    internal void Draw()
    {
    }

    internal void OnGUI()
    {
    }
}