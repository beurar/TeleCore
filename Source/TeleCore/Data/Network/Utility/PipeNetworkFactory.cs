using System.Collections.Generic;
using System.Linq;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using TeleCore.Network.IO;
using TeleCore.Static;
using Verse;

namespace TeleCore.Network.Utility;

public static class PipeNetworkFactory
{
    internal static int MasterNetworkID = 0;

    private static PipeNetwork _curNetwork;
    private static NetworkGraph _curGraph;
    private static NetworkSystem _curNetworkSystem;

    //Graph Generation Steps
    //Look at root
    //Get all adjacent parts
    //  -If it is a node, add a direct edge (0 distance, infinitely small)
    //  -If it is a pipe, search along pipe for next node, start with cell and mode
    //      +Once a viable node is found, try to connect IO with the previously started mode

    /*public static void CreateNetwork(PipeNetworkMaster forMaster, INetworkPart part, out PipeNetwork network)
    {
        _curNetwork = new PipeNetwork(part.Config.networkDef);
        _curNetwork.PrepareForRegen(out _curGraph, out _curNetworkSystem);

        //Graph
        GenerateGraph(part);

        //Populate FlowSystem
        _curNetworkSystem.Notify_Populate(_curGraph);

        network = _curNetwork;
        _curGraph = null;
        _curNetworkSystem = null;
        _curNetwork = null;
    }*/

    private static void RegisterInNetwork(INetworkPart part)
    {
        if (_curNetwork.Notify_AddPart(part))
        {
            part.Network = _curNetwork;
            _curGraph.AddCells(part);
        }
    }

    private static void Notify_FoundEdge(NetEdge edge)
    {
        if (_curGraph.AddEdge(edge))
        {
            RegisterInNetwork(edge.From);
            RegisterInNetwork(edge.To);
        }
    }
    
    private static void GenerateGraph(INetworkPart root)
    {
        //Single root without any neighbors
        if (root.AdjacentSet.Size == 0)
        {
            RegisterInNetwork(root);
            return;
        }
        
        var parts = ContiguousNetworkParts(root);
        var allNodes = parts.Where(n => n.IsNode).ToList();
        
        if (allNodes.Count <= 1)
        {
            foreach (var part in parts)
            {
                RegisterInNetwork(part);
            }
            return;
        }
        
        foreach (var part in allNodes)
        {
            StartAdjacentEdgeSearch(part);
        }
    }

    private static HashSet<INetworkPart> _ClosedGlobalSet;
    
    private static void StartAdjacentEdgeSearch(INetworkPart rootNode)
    {
        _ClosedGlobalSet = StaticListHolder<INetworkPart>.RequestSet("ClosedSubSetGlobalLocal");
        var map = rootNode.Parent.Thing.Map;

        //Add Root To Closed Set
        _ClosedGlobalSet.Add(rootNode);
        
        foreach (var directPart in rootNode.AdjacentSet.FullSet)
        {
            //All directly adjacent nodes get added as infinitely small edges
            if (directPart.IsNode)
            {
                var connResult = rootNode.HasIOConnectionTo(directPart);
                if (connResult)
                {
                    if (connResult.IsBiDirectional)
                    {
                        var edgeBi = new NetEdge(rootNode, directPart, connResult.Out, connResult.In,
                            connResult.OutMode, connResult.InMode, 0);
                        Notify_FoundEdge(edgeBi);
                        Notify_FoundEdge(edgeBi.Reverse);
                        continue;
                    }

                    var edge = new NetEdge(rootNode, directPart, connResult.In, connResult.Out, connResult.InMode,
                        connResult.OutMode, 0);
                    Notify_FoundEdge(edge);
                }
            }
            else if (directPart.IsEdge)
            {
                var directPos = directPart.Parent.Thing.Position;
                InternalEdgeSearch(directPart, directPos, rootNode.PartIO.IOModeAt(directPos), map, rootNode, rootNode);
            }
        }
        _ClosedGlobalSet.Clear();
    }

    private static void InternalEdgeSearch(INetworkPart subPart, IntVec3 startCell, NetworkIOMode startMode, Map? map, INetworkPart rootNode, INetworkPart originPart = null, int? previousLength = null)
    {
        var currentSet = StaticListHolder<INetworkPart>.RequestSet($"CurrentSubSet_{subPart}");
        var openSet = StaticListHolder<INetworkPart>.RequestSet($"OpenSubSet_{subPart}");
        var closedSet = StaticListHolder<INetworkPart>.RequestSet($"ClosedSubSet_{subPart}");

        var mainOriginPart = originPart ?? subPart;
        var curLength = previousLength ?? 1;
        openSet.Add(subPart);

        do
        {
            foreach (var item in openSet)
            {
                closedSet.Add(item);
                _ClosedGlobalSet.Add(item);
                RegisterInNetwork(item);
            }

            (currentSet, openSet) = (openSet, currentSet);
            openSet.Clear();
            
            var copy = new HashSet<INetworkPart>();
            foreach (var cur in currentSet)
            {
                copy.Add(cur);
            }
            foreach (var part in copy)
            {
                foreach (IntVec3 output in part.PartIO.Connections)
                {
                    if (!output.InBounds(map)) continue;
                    List<Thing> thingList = output.GetThingList(map);
                    foreach (var thing in thingList)
                    {
                        if (!Fits(thing, rootNode.Config.networkDef, out var newPart)) continue;
                        if (closedSet.Concat(_ClosedGlobalSet).Contains(newPart)) continue;
                        var result = newPart.HasIOConnectionTo(part);
                        if (result.IsValid)
                        {
                            //Make Edge When Node Found
                            if (newPart.IsNode)
                            {
                                Notify_FoundEdge(new NetEdge(mainOriginPart, newPart, startCell, result.In, startMode, result.InMode, curLength));
                                break;
                            }

                            //Split At Junction (length tracking)
                            if (newPart.IsJunction)
                            {
                                InternalEdgeSearch(newPart, startCell, startMode, map, rootNode, mainOriginPart, curLength);
                                break;
                            }

                            //If Edge, continue search
                            curLength++;
                            openSet.Add(newPart);
                            break;
                        }
                    }
                }
            }
        } while (openSet.Count > 0);

        //
        currentSet.Clear();
        openSet.Clear();
        closedSet.Clear();
    }

    private static void GenerateFlow(NetworkSystem networkSystem)
    {
    }

    private static HashSet<INetworkPart> ContiguousNetworkParts(INetworkPart root)
    {
        var currentSet = StaticListHolder<INetworkPart>.RequestSet("CurrentSubSet");
        var openSet = StaticListHolder<INetworkPart>.RequestSet("OpenSubSet");
        var closedSet = StaticListHolder<INetworkPart>.RequestSet("ClosedSubSet");

        var nodeSet = new HashSet<INetworkPart>();

        var map = root.Parent.Thing.Map;

        closedSet.Clear();
        openSet.Clear();
        currentSet.Clear();
        openSet.Add(root);
        do
        {
            foreach (var item in openSet)
            {
                nodeSet.Add(item);
                closedSet.Add(item);
            }

            (currentSet, openSet) = (openSet, currentSet);
            openSet.Clear();
            foreach (var part in currentSet)
            foreach (IntVec3 output in part.PartIO.Connections)
            {
                if (!output.InBounds(map)) continue;

                List<Thing> thingList = output.GetThingList(map);
                foreach (var thing in thingList)
                {
                    if (!Fits(thing, root.Config.networkDef, out var newPart)) continue;
                    if (closedSet.Contains(newPart)) continue;
                    if (newPart.HasIOConnectionTo(part))
                    {
                        openSet.Add(newPart);
                        break;
                    }
                }
            }
        } while (openSet.Count > 0);

        closedSet.Clear();
        openSet.Clear();
        currentSet.Clear();

        return nodeSet;
    }

    /// <summary>
    /// Checks whether or not a thing is part of a specific network.
    /// </summary>
    internal static bool Fits(Thing thing, NetworkDef network, out INetworkPart part)
    {
        part = null;
        if (thing is not ThingWithComps compThing)
            return false;

        var networkComp = compThing.GetComp<Comp_Network>();
        if (networkComp == null) return false;

        part = networkComp[network];
        return part != null;
    }
}