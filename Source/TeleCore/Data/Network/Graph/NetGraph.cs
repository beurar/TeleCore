using System.Collections;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore.Network;

[StaticConstructorOnStartup]
public class NetGraph
{
    //Graph Data
    private readonly List<INetworkSubPart> _allNodes;
    private readonly Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>> _adjacencyLists;
    private readonly Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge> _edgeLookUp;

    private readonly Dictionary<INetworkSubPart, List<(INetworkSubPart, NetEdge)>> _adjacencyLookUp;

    //Props
    public int NodeCount => _adjacencyLists.Count;
    public int EdgeCount => _edgeLookUp.Count;

    public List<INetworkSubPart> AllNodes => _allNodes;
    public Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge> EdgeLookUp => _edgeLookUp;

    public NetGraph()
    {
        _allNodes = new List<INetworkSubPart>();
        _edgeLookUp = new Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge>();
        _adjacencyLookUp = new Dictionary<INetworkSubPart, List<(INetworkSubPart, NetEdge)>>();
        _adjacencyLists = new Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>>();
    }

    public void Notify_StateChanged(INetworkSubPart part)
    {
    }

    //
    public LinkedList<INetworkSubPart>? GetAdjacencyList(INetworkSubPart forPart)
    {
        if (_adjacencyLists.TryGetValue(forPart, out var list))
        {
            return list;
        }
        return null;
    }
        
    public IEnumerable<(INetworkSubPart,NetEdge)> GetAdjacencyListEdge(INetworkSubPart forPart)
    {
        if (_adjacencyLookUp.TryGetValue(forPart, out var list))
        {
            return list;
        }
        return null;
    }

    public void AddNode(INetworkSubPart node)
    {
        _allNodes.Add(node);
        _adjacencyLists.Add(node, new LinkedList<INetworkSubPart>());
        _adjacencyLookUp.Add(node, new List<(INetworkSubPart, NetEdge)>());
    }

    public bool AddEdge(NetEdge newEdge)
    {
        var newKey = (fromNode: newEdge.startNode, toNode: newEdge.endNode);
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