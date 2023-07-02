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
    internal static readonly HashSet<INetworkPart> _ClosedGlobalSet = new();

    private static PipeNetwork _curNetwork;
    private static NetGraph _curGraph;
    private static FlowSystem _curFlowSystem;

    //Graph Generation Steps
    //Look at root
    //Get all adjacent parts
    //  -If it is a node, add a direct edge (0 distance, infinitely small)
    //  -If it is a pipe, search along pipe for next node, start with cell and mode
    //      +Once a viable node is found, try to connect IO with the previously started mode

    public static void CreateNetwork(INetworkPart part, out PipeNetwork network)
    {
        _curNetwork = new PipeNetwork(part.Config.networkDef);
        _curNetwork.PrepareForRegen(out _curGraph, out _curFlowSystem);

        //Graph
        GenerateGraph(part);


        //Populate FlowSystem
        _curFlowSystem.Notify_Populate(_curGraph);

        network = _curNetwork;
        _curGraph = null;
        _curFlowSystem = null;
        _curNetwork = null;
    }

    private static void Notify_FoundConsecutivePart(INetworkPart part)
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
        }
    }

    private static void GenerateGraph(INetworkPart root)
    {
        var allParts = ContiguousNetworkParts(root).Where(n => n.IsNode);
        foreach (var part in allParts)
            //Register part in network
            //AddNetworkDataCallback(part, network);
            StartAdjacentEdgeSearch(part);
    }

    private static void FindClosesReachableNode(INetworkPart edgeNode)
    {
        var closedLocalSet = StaticListHolder<INetworkPart>.RequestSet("ClosedSubSetGlobalLocal");
    }

    private static void StartAdjacentEdgeSearch(INetworkPart rootNode)
    {
        var closedSetGlobalLocal = StaticListHolder<INetworkPart>.RequestSet("ClosedSubSetGlobalLocal");
        var map = rootNode.Parent.Thing.Map;

        //Add Root To Closed Set
        closedSetGlobalLocal.Add(rootNode);

        //All directly adjacent parts get added as infinitely small edges
        foreach (var directPart in rootNode.AdjacentSet.FullSet)
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
                InternalEdgeSearch(directPart, directPos, rootNode.PartIO.IOModeAt(directPos), rootNode);
            }

        void InternalEdgeSearch(INetworkPart subPart, IntVec3 startCell, NetworkIOMode startMode,
            INetworkPart originPart = null, int? previousLength = null)
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
                    closedSetGlobalLocal.Add(item);
                    Notify_FoundConsecutivePart(item);
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
                        if (!Fits(thing, rootNode.Config.networkDef, out var newPart)) continue;
                        if (closedSet.Concat(closedSetGlobalLocal).Contains(newPart)) continue;
                        var result = newPart.HasIOConnectionTo(part);
                        if (result.IsValid)
                        {
                            //Make Edge When Node Found
                            if (newPart.IsNode)
                            {
                                Notify_FoundEdge(new NetEdge(mainOriginPart, newPart, startCell, result.In, startMode,
                                    result.InMode, curLength));
                                break;
                            }

                            //Split At Junction (length tracking)
                            if (newPart.IsJunction)
                            {
                                InternalEdgeSearch(newPart, startCell, startMode, mainOriginPart, curLength);
                                break;
                            }

                            //If Edge, continue search
                            curLength++;
                            openSet.Add(newPart);
                            break;
                        }
                    }
                }
            } while (openSet.Count > 0);

            //
            currentSet.Clear();
            openSet.Clear();
            closedSet.Clear();
        }

        //    
        closedSetGlobalLocal.Clear();
    }

    private static void GenerateFlow(FlowSystem flowSystem)
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
                if (item.IsNode) nodeSet.Add(item);
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
    ///     Checks whether or not a thing is part of a specific network.
    /// </summary>
    internal static bool Fits(Thing thing, NetworkDef network, out INetworkPart? part)
    {
        part = null;
        if (thing is not ThingWithComps compThing)
            return false;

        var networkComp = compThing.GetComp<Comp_Network>();
        if (networkComp == null)
            return false;

        part = networkComp[network];
        return part != null;
    }
}