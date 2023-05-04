using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TeleCore.Data.Network;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public static class PipeNetworkMaker
    {
        internal static readonly HashSet<INetworkSubPart> _ClosedGlobalSet = new();

        internal static HashSet<INetworkSubPart> _OpenSubSet = new();
        internal static HashSet<INetworkSubPart> _CurrentSubSet = new();

        public static PipeNetwork RegenerateNetwork(INetworkSubPart root, PipeNetworkSystem system)
        {
            PipeNetwork newNet = new PipeNetwork(root.NetworkDef, system);
            GenerateGraph(root, newNet);
            newNet.Initialize();
            return newNet;
        }

        public static NetworkGraph MakeGraphNotFinal(PipeNetwork withNetwork)
        {
            NetworkGraph newGraph = new NetworkGraph();
            newGraph.ParentNetwork = withNetwork;
            withNetwork.Graph = newGraph;
            return newGraph;
        }
        
        //Spawn Node
        //Check adjacent cells for nodes
        //Otherwise check pipe connections
        public static void GenerateGraph(INetworkSubPart root, PipeNetwork forNetwork)
        {
            NetworkGraph newGraph = MakeGraphNotFinal(forNetwork);

            //Generate Network and Graph
            _ClosedGlobalSet.Clear();
            
            //
            CreateGraph(root, newGraph);

            _ClosedGlobalSet.Clear();
        }
        
        public static void CreateGraph(INetworkSubPart fromRoot, NetworkGraph graph)
        {
            //Get all connected parts
            var allNodes = ContiguousNetworkParts(fromRoot);
            foreach (var subRootPart in allNodes)
            {
                //Register part in network
                AddNetworkDataCallback(subRootPart, graph.ParentNetwork);
                
                //Set edges for part
                var allEdges = GetAllAdjacencyEdges(subRootPart, graph);
                foreach (var netEdge in allEdges)
                {
                    if (netEdge.IsValid)
                    {
                        if (TrySetEdge(netEdge, graph))
                        {

                        }
                    }
                }
            }
        }
        
        private static List<INetworkSubPart> ContiguousNetworkParts(INetworkSubPart root)
        {
            var currentSet = StaticListHolder<INetworkSubPart>.RequestSet("CurrentSubSet");
            var openSet = StaticListHolder<INetworkSubPart>.RequestSet("OpenSubSet");
            var closedSet = StaticListHolder<INetworkSubPart>.RequestSet("ClosedSubSet");

            var nodeList = new List<INetworkSubPart>();
            
            var map = root.Parent.Thing.Map;
            
            closedSet.Clear();
            openSet.Clear();
            currentSet.Clear();
            openSet.Add(root);
            do
            {
                foreach (var item in openSet)
                {
                    if (item.IsNetworkNode)
                    {
                        nodeList.Add(item);
                    }
                    closedSet.Add(item);
                }
                (currentSet, openSet) = (openSet, currentSet);
                openSet.Clear();
                foreach (INetworkSubPart part in currentSet)
                {
                    foreach (IntVec3 output in part.CellIO.OuterConnnectionCells)
                    {
                        if(!output.InBounds(map)) continue;
                        
                        List<Thing> thingList = output.GetThingList(map);
                        foreach (var thing in thingList)
                        {
                            if (!Fits(thing, root.NetworkDef, out var newPart)) continue;
                            if (closedSet.Contains(newPart)) continue;
                            if (newPart.ConnectsTo(part))
                            {
                                openSet.Add(newPart);
                                break;
                            }
                        }
                    }
                }
            }
            while (openSet.Count > 0);
            
            closedSet.Clear();
            openSet.Clear();
            currentSet.Clear();

            return nodeList;
        }

        private static List<NetEdge> GetAllAdjacencyEdges(INetworkSubPart rootNode, NetworkGraph graph)
        {
            var closedSetGlobalLocal = StaticListHolder<INetworkSubPart>.RequestSet("ClosedSubSetGlobalLocal");
            var map = rootNode.Parent.Thing.Map;
            var resultList = new List<NetEdge>();
            
            //
            closedSetGlobalLocal.Add(rootNode);
            foreach (var directPart in rootNode.DirectPartSet.FullSet)
            {
                if (directPart.IsNetworkNode)
                {
                    
                }
                else if (directPart.IsNetworkEdge)
                {
                    var directPos = directPart.Parent.Thing.Position;
                    InternalEdgeSearch(directPart, directPos, rootNode.CellIO.IOModeFor(directPos), rootNode);
                }
            }

            void InternalEdgeSearch(INetworkSubPart subPart, IntVec3 startCell, NetworkIOMode startMode, INetworkSubPart originPart = null, int? previousLength = null)
            {
                var currentSet = StaticListHolder<INetworkSubPart>.RequestSet($"CurrentSubSet_{subPart}");
                var openSet = StaticListHolder<INetworkSubPart>.RequestSet($"OpenSubSet_{subPart}");
                var closedSet = StaticListHolder<INetworkSubPart>.RequestSet($"ClosedSubSet_{subPart}");
                
                var mainOriginPart = originPart ?? subPart;
                var curLength = previousLength ?? 1;
                
                openSet.Add(subPart);
                do
                {
                    foreach (var item in openSet)
                    {
                        closedSet.Add(item);
                        AddNetworkDataCallback(item, graph.ParentNetwork);
                        closedSetGlobalLocal.Add(item);
                    }
                    (currentSet, openSet) = (openSet, currentSet);
                    openSet.Clear();
                    foreach (INetworkSubPart part in currentSet)
                    {
                        foreach (IntVec3 output in part.CellIO.OuterConnnectionCells)
                        {
                            if(!output.InBounds(map)) continue;
                            List<Thing> thingList = output.GetThingList(map);
                            foreach (var thing in thingList)
                            {
                                if (!Fits(thing, rootNode.NetworkDef, out var newPart)) continue;
                                if (closedSet.Concat(closedSetGlobalLocal).Contains(newPart)) continue;
                                if (newPart.ConnectsTo(part, out var cell, out var IOMode))
                                {
                                     //Make Edge When Node Found
                                    if (newPart.IsNetworkNode)
                                    {
                                        resultList.Add(new NetEdge(mainOriginPart, newPart, startCell, cell, startMode,IOMode, curLength));
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
                    }
                }
                while (openSet.Count > 0);
                
                //
                currentSet.Clear();
                openSet.Clear();
                closedSet.Clear();
            }

            //    
            closedSetGlobalLocal.Clear();
            return resultList;
        }
        
        private static void AddNetworkDataCallback(INetworkSubPart newPart, PipeNetwork network)
        {
            _ClosedGlobalSet.Add(newPart);
            newPart.Network = network;
            network.Notify_AddPart(newPart);
        }

        private static bool TrySetEdge(NetEdge newEdge, NetworkGraph forGraph)
        {
            //Add Edge To Graph with found nodes
             if (forGraph.AddEdge(newEdge))
            {
                newEdge.startNode.Notify_SetConnection(newEdge, newEdge.fromCell);
                newEdge.endNode.Notify_SetConnection(newEdge, newEdge.toCell);
                return true;
            }
            return false;
        }
        
        //Check whether or not a thing is part of a network
        internal static bool Fits(Thing thing, NetworkDef forNetwork, out INetworkSubPart part)
        {
            part = null;
            if (thing is not ThingWithComps compThing) return false;
            
            var structure = compThing.GetComp<Comp_Network>();
            if (structure == null) return false;
            
            part = structure[forNetwork];
            return part != null;
        }
    }
}
