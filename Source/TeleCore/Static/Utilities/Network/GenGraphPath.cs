using System;
using System.Collections.Generic;
using TeleCore.Network;
using TeleCore.Network.Data;
using Verse;

namespace TeleCore.Static.Utilities;

internal class GenGraphPath
{
    private static List<INetworkPart> _WorkingList = new();
    private static Dictionary<INetworkPart, int> _Distances = new();
    private static Dictionary<INetworkPart, INetworkPart> _PreviousOf = new();
        
    public static List<INetworkPart> Djikstra_Single(NetGraph graph, INetworkPart source, Predicate<INetworkPart> validator, int maxDepth = int.MaxValue)
    {
        //
        _WorkingList.Clear();
        _Distances.Clear();
        _PreviousOf.Clear();

        //Current
        List<INetworkPart> validParts = new List<INetworkPart>();
            
        //Setup
        for (var k = 0; k < graph.AllNodes.Count; k++)
        {
            var node = graph.AllNodes[k];
            _WorkingList.Add(node);
            _PreviousOf.Add(node, null);
            if (validator(node))
            {
                validParts.Add(node);
            }

            if (node == source)
            {
                _Distances.Add(node, 0);
                continue;
            }

            _Distances.Add(node, int.MaxValue);
        }

        //If no targets
        if (validParts.Count <= 0) return null;

        //Traverse Until Valid Part Found
        while (_WorkingList.Count > 0)
        {
            //Check current state for end
            INetworkPart part = null;
            if (validParts.Any())
            {
                foreach (var toPart in validParts)
                {
                    part = toPart;
                    if (_PreviousOf[part] != null || part == source)
                    {
                        List<INetworkPart> pathResult = new List<INetworkPart>();
                        var depthCount = 0;
                        while (part != null)
                        {
                            pathResult.Insert(0, part);
                            part = _PreviousOf[part];
                            depthCount++;
                            if (depthCount > maxDepth)
                            {
                                goto _ExitLoop;
                            }
                        }
                            
                        //
                        _WorkingList.Clear();
                        _Distances.Clear();
                        _PreviousOf.Clear();
                        return pathResult;
                    }
                        
                    //
                    _ExitLoop: ;
                }
            }

            //
            part = _WorkingList.MinBy(v => _Distances[v]);

            _WorkingList.Remove(part);
            var adjacencyList = graph.GetAdjacencyList(part);
            foreach (var neighbor in adjacencyList)
            {
                if (!_WorkingList.Contains(neighbor)) continue;
                if (graph.TryGetEdge(part, neighbor, out var edge))
                {
                    if (!neighbor.CanTransmit(edge)) continue;
                    var alt = _Distances[part] + edge._weight;
                    if (alt < _Distances[neighbor])
                    {
                        _Distances[neighbor] = alt;
                        _PreviousOf[neighbor] = part;
                    }
                }
            }
        }

        //
        _WorkingList.Clear();
        _Distances.Clear();
        _PreviousOf.Clear();
        return null;
    }

    public static List<INetworkPart> GetPaths()
    {
        return null;
    }

    public static List<List<INetworkPart>> Dijkstra(NetworkGraph graph, INetworkPart source, Predicate<INetworkPart> validator, int maxDepth = int.MaxValue)
    {
        //
        _WorkingList.Clear();
        _Distances.Clear();
        _PreviousOf.Clear();

        //
        List<INetworkPart> validParts = new List<INetworkPart>();
        List<List<INetworkPart>> allPaths = new List<List<INetworkPart>>();

        bool Validator(INetworkPart part) => validator(part) && part != source;

        //
        for (var k = 0; k < graph.AllNodes.Count; k++)
        {
            var node = graph.AllNodes[k];
            _WorkingList.Add(node);
            _PreviousOf.Add(node, null);

            if (Validator(node))
            {
                validParts.Add(node);
            }

            if (node == source)
            {
                _Distances.Add(node, 0);
                continue;
            }
            _Distances.Add(node, int.MaxValue);
        }

        while (_WorkingList.Count > 0)
        {
            //
            INetworkPart part = null;
            if (validParts.Any())
            {
                foreach (var toPart in validParts)
                {
                    part = toPart;
                    if (_PreviousOf[part] != null || (_PreviousOf[part] == null && part == source))
                    {
                        List<INetworkPart> pathResult = new List<INetworkPart>();
                        var depthCount = 0;
                        while (part != null)
                        {
                            if(Validator(part))
                                depthCount++;
                                
                            pathResult.Insert(0, part);

                            if (part == source)
                            {
                                allPaths.Add(pathResult);
                                break;
                            }
                                
                            part = _PreviousOf[part];
                            if (depthCount > maxDepth)
                            {
                                goto _ExitLoop;
                            }
                        }
                    }
                    _ExitLoop: ;
                }
                if(allPaths.Count == validParts.Count)
                    return allPaths;
            }

            //
            part = _WorkingList.MinBy(v => _Distances[v]);

            _WorkingList.Remove(part);
            var adjacencyList = graph.GetAdjacencyList(part);
            foreach (var neighbor in adjacencyList)
            {
                if (!_WorkingList.Contains(neighbor)) continue;
                if (graph.TryGetEdge(part, neighbor, out var edge))
                {
                    var alt = _Distances[part] + edge._weight;
                    if (alt < _Distances[neighbor])
                    {
                        _Distances[neighbor] = alt;
                        _PreviousOf[neighbor] = part;
                    }
                }
            }
        }

        //
        _WorkingList.Clear();
        _Distances.Clear();
        _PreviousOf.Clear();
        return allPaths.Count > 0 ? allPaths : null;
    }
}