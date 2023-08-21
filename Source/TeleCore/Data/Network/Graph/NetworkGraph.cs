using System;
using System.Collections.Generic;
using System.Diagnostics;
using TeleCore.Generics;
using TeleCore.Network.Data;
using UnityEngine;
using Verse;

namespace TeleCore.Network.Graph;

[DebuggerDisplay("{Nodes.Count} | {Edges.Count}")]
public class NetworkGraph : IDisposable
{
    public NetworkGraph()
    {
        Nodes = new HashSet<NetNode>();
        Edges = new HashSet<NetEdge>();
        AdjacencyList = new Dictionary<NetNode, List<(NetEdge, NetNode)>>();
        EdgeLookUp = new Dictionary<TwoWayKey<NetNode>, NetEdge>();
        //Cells = new List<IntVec3>();
    }

    public HashSet<NetNode> Nodes { get; private set; }
    public HashSet<NetEdge> Edges { get; private set; }
    public Dictionary<NetNode, List<(NetEdge, NetNode)>> AdjacencyList { get; private set; }
    public Dictionary<TwoWayKey<NetNode>, NetEdge> EdgeLookUp { get; private set; }

    //public List<IntVec3> Cells { get; private set; }

    public void Dispose()
    {
        //Cells.Clear();
        Nodes.Clear();
        Edges.Clear();
        AdjacencyList.Clear();
        EdgeLookUp.Clear();

        //Cells = null;
        Nodes = null;
        Edges = null;
        AdjacencyList = null;
        EdgeLookUp = null;
    }

    public void DissolveEdge(NetworkPart from, NetworkPart to)
    {
        var key = new TwoWayKey<NetNode>(from, to);
        if (EdgeLookUp.TryGetValue(key, out var edge))
        {        
            Edges.Remove(edge);
            EdgeLookUp.Remove(edge);
            if (AdjacencyList.TryGetValue(edge.From, out var fromList))
            {
                fromList.RemoveAll(e => e.Item2.Value == edge.To);
            }
            if (AdjacencyList.TryGetValue(edge.To, out var toList))
            {
                toList.RemoveAll(e => e.Item2.Value == edge.From);
            }
        }
    }

    public void DissolveEdge(NetEdge edge)
    {
        DissolveEdge(edge.From, edge.To);
    }

    public bool TryDissolveNode(NetworkPart node)
    {
        if (!Nodes.Contains(node)) return false;
        
        Nodes.Remove(node);
        if (AdjacencyList.TryGetValue(node, out var list))
        {
            foreach (var (edge, _) in list)
            {
                Edges.Remove(edge);
                EdgeLookUp.Remove((edge.From, edge.To));
            }

            AdjacencyList.Remove(node);
        }

        return true;
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
            //Cells.Add(cell);
        }
    }
    
    internal bool AddEdge(NetEdge edge)
    {
        //Ignore invalid edges
        if (!edge.IsValid) return false;

        if (Edges.Add(edge))
        {
            var key = new TwoWayKey<NetNode>(edge.From, edge.To);
            if (EdgeLookUp.TryAdd(key, edge))
            {
                Nodes.Add(edge.From);
                Nodes.Add(edge.To);

                TryAddAdjacency(edge.From, edge.To, edge);
                TryAddAdjacency(edge.To, edge.From, edge);
            }
        }
        return true;
    }

    private void TryAddAdjacency(NetNode nodeFrom, NetNode nodeTo, NetEdge edge)
    {
        if (!AdjacencyList.TryGetValue(nodeFrom, out var listSource))
        {
            listSource = new List<(NetEdge, NetNode)>()
            {
                (edge, nodeTo)
            };
            AdjacencyList.Add(nodeFrom, listSource);
        }
        else
        {
            listSource.Add((edge, nodeTo));
        }
    }

    internal void Draw()
    {
        //GenDraw.DrawFieldEdges(Cells, Color.cyan);
    }

    internal void OnGUI()
    {
        if (TeleCoreDebugViewSettings.DrawGraphOnGUI)
        {
            Static.Utilities.DebugTools.Debug_DrawGraphOnUI(this);
        }
    }
}