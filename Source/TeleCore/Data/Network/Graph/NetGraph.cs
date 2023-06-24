using System.Collections.Generic;

namespace TeleCore.Network.Graph;

public class NetGraph
{
    private List<NetNode> _nodes;
    private List<NetEdge> _edges;
    private Dictionary<NetNode, List<(NetEdge, NetNode)>> _adjacencyList;
    private Dictionary<(NetNode, NetNode), NetEdge> _edgeLookUp;

    public List<NetNode> Nodes => _nodes;
    public List<NetEdge> Edges => _edges;
    public Dictionary<NetNode, List<(NetEdge, NetNode)>> AdjacencyList => _adjacencyList;
    public Dictionary<(NetNode, NetNode), NetEdge> EdgeLookUp => _edgeLookUp;

    public NetGraph()
    { 
        _nodes = new List<NetNode>();
        _edges = new List<NetEdge>();
        _adjacencyList = new Dictionary<NetNode, List<(NetEdge, NetNode)>>();
        _edgeLookUp = new Dictionary<(NetNode, NetNode), NetEdge>();
    }

    private void AddNode(NetNode node)
    {
        Nodes.Add(node);
        AdjacencyList.Add(node, new());
    }

    public bool AddEdge(NetEdge edge)
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
        if (!AdjacencyList.TryGetValue(edge.From, out var listSource))
        {
            AddNode(edge.From);
            listSource = AdjacencyList[edge.From];
        }
        
        if (!listSource.Contains((edge, edge.To)))
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