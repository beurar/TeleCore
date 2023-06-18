using System.Collections;
using System.Collections.Generic;
using RimWorld;
using TeleCore.Network.Graph;
using UnityEngine;
using Verse;

namespace TeleCore.Network;

[StaticConstructorOnStartup]
public class NetGraph
{
    //Graph Data
    private readonly List<NetNode> _allNodes;
    private readonly Dictionary<NetNode, LinkedList<NetNode>> _adjacencyLists;
    private readonly Dictionary<(NetNode, NetNode), NetEdge> _edgeLookUp;

    private readonly Dictionary<NetNode, List<(NetNode, NetEdge)>> _adjacencyLookUp;

    //Props
    public int NodeCount => _adjacencyLists.Count;
    public int EdgeCount => _edgeLookUp.Count;

    public List<NetNode> AllNodes => _allNodes;
    public Dictionary<(NetNode, NetNode), NetEdge> EdgeLookUp => _edgeLookUp;

    public NetGraph()
    {
        _allNodes = new List<NetNode>();
        _edgeLookUp = new Dictionary<(NetNode, NetNode), NetEdge>();
        _adjacencyLookUp = new Dictionary<NetNode, List<(NetNode, NetEdge)>>();
        _adjacencyLists = new Dictionary<NetNode, LinkedList<NetNode>>();
    }

    public void Notify_StateChanged(INetworkSubPart part)
    {
    }

    //
    public LinkedList<NetNode>? GetAdjacencyList(NetworkSubPart forPart)
    {
        if (_adjacencyLists.TryGetValue(forPart, out var list))
        {
            return list;
        }
        return null;
    }
        
    public IEnumerable<(NetNode,NetEdge)> GetAdjacencyListEdge(NetworkSubPart forPart)
    {
        if (_adjacencyLookUp.TryGetValue(forPart, out var list))
        {
            return list;
        }
        return null;
    }

    public void AddNode(NetNode node)
    {
        _allNodes.Add(node);
        _adjacencyLists.Add(node, new LinkedList<NetNode>());
        _adjacencyLookUp.Add(node, new List<(NetNode, NetEdge)>());
    }

    public bool AddEdge(NetEdge newEdge)
    {
        var newKey = (fromNode: (NetNode)(NetworkSubPart)newEdge.startNode, toNode: (NetNode)(NetworkSubPart)newEdge.endNode);
        if (_edgeLookUp.ContainsKey(newKey))
        {
            TLog.Warning($"Key ({newEdge.startNode.Parent.Thing}, {newEdge.endNode.Parent.Thing}) already exists in graph!");
            return false;
        }

        if (newEdge.IsValid)
        {
            _edgeLookUp.Add(newKey, newEdge);
            if (!_adjacencyLists.TryGetValue(newEdge.startNode, out var listSource))
            {
                AddNode(newEdge.startNode);
                listSource = _adjacencyLists[newEdge.startNode];
            }
            if (!listSource.Contains(newEdge.endNode))
            {
                listSource.AddFirst(newEdge.endNode);
                _adjacencyLookUp[newEdge.startNode].Add((newEdge.endNode, newEdge));
            }
        }
        return true;
    }
    
    //
    public bool TryGetEdge(INetworkSubPart source, INetworkSubPart dest, out NetEdge value)
    {
        value = GetEdgeFor(source, dest);
        return value.IsValid;
        return _edgeLookUp.TryGetValue((source, dest), out value);// || _edges.TryGetValue((dest, source), out value);
    }

    private NetEdge GetEdgeFor(INetworkSubPart source, INetworkSubPart dest, bool any = false)
    {
        if (_edgeLookUp.TryGetValue((source, dest), out var value))
        {
            return value;
        }

        if (_edgeLookUp.TryGetValue((dest, source), out value))
        {
            return any ? value : value.Reverse;
        }

        return NetEdge.Invalid;
    }
}