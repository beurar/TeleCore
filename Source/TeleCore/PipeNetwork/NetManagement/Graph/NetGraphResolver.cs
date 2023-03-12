using System;
using System.Collections.Generic;
using static System.String;

namespace TeleCore;

public static class NetGraphResolver
{
    private static readonly Dictionary<INetworkSubPart, HashSet<INetworkSubPart>> foundNodesBySearcher = new();
    private static readonly Dictionary<INetworkSubPart, HashSet<INetworkSubPart>> travelledJunctionsBySearcher = new();

    public static void ResolveGraph(NetworkGraph graph, INetworkSubPart fromNode)
    {
        NetworkGraph newGraph = PipeNetworkMaker.MakeGraphNotFinal(graph.ParentNetwork);
        foundNodesBySearcher.Clear();
        travelledJunctionsBySearcher.Clear();
        
        void CallBack((INetworkSubPart, INetworkSubPart) tuple)
        {
            newGraph.AddEdge(new NetEdge(tuple.Item1, tuple.Item2));
        }

        //
        TryStartResolveAt(fromNode, graph, CallBack);

        foundNodesBySearcher.Clear();
        travelledJunctionsBySearcher.Clear();
    }
    
    public static void TryStartResolveAt(INetworkSubPart node, NetworkGraph inGraph, Action<(INetworkSubPart, INetworkSubPart)> edgeFoundCallBack, INetworkSubPart initialSearcher = null)
    {
        var searcher = initialSearcher ?? node;
        var travelledSet = TravelledJunctionsFor(searcher);
        var foundNodes = FoundNodesFor(searcher);
        
        foundNodes.Add(searcher);
        var adjacencyList = inGraph.GetAdjacencyList(node);
        if (adjacencyList == null)
        {
            TLog.Warning($"Root {node} has no adjacent connections!");
            return;
        }
        
        //var listCopy = adjacencyList.ToList();
        //Look at adjacent nodes
        foreach (var subPart in adjacencyList)
        {
            //If adjacent is junction - look deeper
            if (subPart.IsJunction)
            {
                if(travelledSet.Contains(subPart)) continue;
                travelledSet.Add(subPart);
                TryStartResolveAt(subPart, inGraph, edgeFoundCallBack, searcher);
            }
            else if (subPart.IsNetworkNode)
            {
                if(foundNodes.Contains(subPart)) continue;
                foundNodes.Add(subPart);
                edgeFoundCallBack.Invoke((searcher, subPart));
                TryStartResolveAt(subPart, inGraph, edgeFoundCallBack);
            }
        }
        
        /*
        for (var i = adjacencyList.Count - 1; i >= 0; i--)
        {
            var adjacentPart = listCopy[i];
            
            //If adjacent is junction - look deeper
            if (adjacentPart.IsJunction)
            {
                if(travelledSet.Contains(adjacentPart)) continue;
                travelledSet.Add(adjacentPart);
                TryStartResolveAt(adjacentPart, inGraph, edgeFoundCallBack, searcher);
            }
            else if (adjacentPart.IsNetworkNode)
            {
                if(foundNodes.Contains(adjacentPart)) continue;
                foundNodes.Add(adjacentPart);
                edgeFoundCallBack.Invoke((searcher, adjacentPart));
                TryStartResolveAt(adjacentPart, inGraph, edgeFoundCallBack);
            }
        }
        */
    }

    private static string GenStringFrom(INetworkSubPart node, List<INetworkSubPart> list)
    {
        var start = $"[{node}]\n    |\n    |";
        var pattern = " \n    |\n    |----{0}";

        foreach (var part in list)
        {
            start += Format(pattern, part);
            
        }
        return start;
    }
    
    private static HashSet<INetworkSubPart> FoundNodesFor(INetworkSubPart part)
    {
        if (foundNodesBySearcher.TryGetValue(part, out var set)) return set;
        set = new HashSet<INetworkSubPart>();
        foundNodesBySearcher.Add(part, set);
        return set;
    }

    private static HashSet<INetworkSubPart> TravelledJunctionsFor(INetworkSubPart part)
    {
        if (travelledJunctionsBySearcher.TryGetValue(part, out var set)) return set;
        set = new HashSet<INetworkSubPart>();
        travelledJunctionsBySearcher.Add(part, set);
        return set;
    }
}