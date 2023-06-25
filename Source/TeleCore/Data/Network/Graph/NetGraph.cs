using System;
using System.Collections.Generic;
using TeleCore.Network.Data;
using Verse;

namespace TeleCore.Network.Graph;

public class NetGraph : IDisposable
{
    private List<IntVec3> _cells;
    private List<NetNode> _nodes;
    private List<NetEdge> _edges;
    private Dictionary<NetNode, List<(NetEdge, NetNode)>> _adjacencyList;
    private Dictionary<(NetNode, NetNode), NetEdge> _edgeLookUp;

    public List<NetNode> Nodes => _nodes;
    public List<NetEdge> Edges => _edges;
    public Dictionary<NetNode, List<(NetEdge, NetNode)>> AdjacencyList => _adjacencyList;
    public Dictionary<(NetNode, NetNode), NetEdge> EdgeLookUp => _edgeLookUp;

    public List<IntVec3> Cells => _cells;

    public NetGraph()
    { 
        _nodes = new List<NetNode>();
        _edges = new List<NetEdge>();
        _adjacencyList = new Dictionary<NetNode, List<(NetEdge, NetNode)>>();
        _edgeLookUp = new Dictionary<(NetNode, NetNode), NetEdge>();
    }

    public void Dispose()
    {
        _cells.Clear();
        _nodes.Clear();
        _edges.Clear();
        _adjacencyList.Clear();
        _edgeLookUp.Clear();
        
        _cells = null;
        _nodes = null;
        _edges = null;
        _adjacencyList = null;
        _edgeLookUp = null;
    }
    
    private void AddNode(NetNode node, NetEdge fromEdge)
    {
        Nodes.Add(node);
        AdjacencyList.Add(node, new());
    }

    internal void AddCells(INetworkPart netPart)
    {
        foreach (var cell in netPart.Thing.OccupiedRect())
        {
            _cells.Add(cell);
        }
    }
    
    internal bool AddEdge(NetEdge edge)
    {
        //Ignore invalid edges
        if (!edge.IsValid) return true;
        
        //Check existing
        var key = (fromNode: (NetNode)edge.From, toNode: (NetNode)edge.To);
        if (_edgeLookUp.ContainsKey(key))
        {
            TLog.Warning($"Key ({edge.From}, {edge.To}) already exists in graph!");
            return false;
        }
        
        _edgeLookUp.Add(key, edge);
        if (!AdjacencyList.TryGetValue(edge.From, out var listSource)) //Check if node exists to have any adj nghbrs
        {
            AddNode(edge.From, edge); //Add starting node as known node
            listSource = AdjacencyList[edge.From];
        }
        
        if (!listSource.Contains((edge, edge.To))) //Check if edge is already int adjancy list for starting node (edge.From)
        {
            listSource.Add((edge, edge.To));
            AdjacencyList[edge.From].Add((edge, edge.To));
        }
        return true;
    }

    internal void Draw()
    {
        throw new System.NotImplementedException();
    }

    internal void OnGUI()
    {
        throw new System.NotImplementedException();
    }
}