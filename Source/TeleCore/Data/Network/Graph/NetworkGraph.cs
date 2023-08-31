using System;
using System.Collections.Generic;
using System.Diagnostics;
using TeleCore.Generics;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using UnityEngine;
using Verse;

namespace TeleCore.Network.Graph;

[DebuggerDisplay("{Nodes.Count} | {Edges.Count}")]
public class NetworkGraph : IDisposable
{
    public NetworkGraph()
    {
        Nodes = new HashSet<NetNode>();
        AdjacencyList = new Dictionary<NetNode, List<(NetEdge, NetNode)>>();
        UniqueEdges = new Dictionary<TwoWayKey<IOConnection>, NetEdge>();
        EdgesByNodes = new Dictionary<TwoWayKey<NetworkPart>, List<NetEdge>>();
        //Cells = new List<IntVec3>();
    }

    public HashSet<NetNode> Nodes { get; private set; }
    //public HashSet<NetEdge> Edges { get; private set; }
    public Dictionary<NetNode, List<(NetEdge, NetNode)>> AdjacencyList { get; private set; }
    

    public Dictionary<TwoWayKey<IOConnection>, NetEdge> UniqueEdges { get; private set; }
    public Dictionary<TwoWayKey<NetworkPart>, List<NetEdge>> EdgesByNodes { get; set; }
    
    private bool RegisterEdge(NetEdge edge)
    {
        var edgeKey = (edge.FromAnchor, edge.ToAnchor);
        if (UniqueEdges.TryAdd(edgeKey, edge))
        {
            var nodeKey = new TwoWayKey<NetworkPart>(edge.From, edge.To);
            GetOrMakeEdgeBag(nodeKey, out var edges);
            edges.Add(edge);
            return true;
        }
        return false;
    }

    private void DeregisterEdge(NetEdge edge)
    {
        var edgeKey = (edge.FromAnchor, edge.ToAnchor);
        if (UniqueEdges.Remove(edgeKey))
        {
            var nodeKey = new TwoWayKey<NetworkPart>(edge.From, edge.To);
            EdgesByNodes.Remove(nodeKey);
            // GetOrMakeEdgeBag(nodeKey, out var edges);
            // edges.Clear();
        }
    }

    private void GetOrMakeEdgeBag(TwoWayKey<NetworkPart> key, out List<NetEdge> edges)
    {
        if (!EdgesByNodes.TryGetValue(key, out edges))
        {
            edges = new List<NetEdge>();
            EdgesByNodes.Add(key, edges);
        }
    }

    //public List<IntVec3> Cells { get; private set; }

    public void Dispose()
    {
        //Cells.Clear();
        Nodes.Clear();
        AdjacencyList.Clear();
        EdgesByNodes.Clear();
        UniqueEdges.Clear();

        //Cells = null;
        Nodes = null;
        AdjacencyList = null;
        EdgesByNodes = null;
        UniqueEdges = null;
    }
    
    public NetEdge GetBestEdgeFor(TwoWayKey<NetworkPart> nodes)
    {
        return EdgesByNodes.TryGetValue(nodes, out var edges) ? edges.FirstOrFallback() : NetEdge.Invalid;
    }

    public void TryDissolveEdge(IOConnection fromAnchor, IOConnection toAnchor)
    {
        var key = new TwoWayKey<IOConnection>(fromAnchor, toAnchor);
        if (UniqueEdges.TryGetValue(key, out var edge))
        {
            if (EdgeFits(edge, fromAnchor, toAnchor))
            {
                DeregisterEdge(edge);
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
    }

    private static bool EdgeFits(NetEdge edge, IOConnection fromAnchor, IOConnection toAnchor)
    {
        var from = edge.FromAnchor;
        var to = edge.ToAnchor;
        var preResult1 = from.Equals(fromAnchor) && to.Equals(toAnchor);
        var preResult2 = from.Equals(toAnchor) && to.Equals(fromAnchor);
        return preResult1 || preResult2;
    }
    
    public void TryDissolveEdge(NetworkPart from, NetworkPart to)
    {
        var key = new TwoWayKey<NetworkPart>(from, to);
        if (EdgesByNodes.TryGetValue(key, out var edges))
        {
            foreach (var edge in edges)
            {
                DeregisterEdge(edge);
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
    }

    public bool TryDissolveNode(NetworkPart node)
    {
        if (!Nodes.Contains(node)) return false;
        
        Nodes.Remove(node);
        if (AdjacencyList.TryGetValue(node, out var list))
        {
            foreach (var (edge, _) in list)
            {
                DeregisterEdge(edge);
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

        if (RegisterEdge(edge))
        {
            Nodes.Add(edge.From);
            Nodes.Add(edge.To);

            TryAddAdjacency(edge.From, edge.To, edge);
            TryAddAdjacency(edge.To, edge.From, edge);
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