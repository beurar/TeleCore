using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public static PipeNetwork RegenerateNetwork(INetworkSubPart root, PipeNetworkManager manager)
        {
            TLog.Message($"Creating Network from {root} for {manager.NetworkDef}");
            PipeNetwork newNet = new PipeNetwork(root.NetworkDef, manager);

            //
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
            return;
            //Search For Initial Node
            if (root.IsNetworkEdge)
            {
                root = FindNextNodeAlongEdge(root);
                if (root == null)
                {
                    TLog.Warning($"Could not find a suitable Network Root for {forNetwork.Def}");
                    return;
                }
            }

            //Start Graph Creation
            Stopwatch watch = new Stopwatch();
            watch.Start();
            InitGraphSearch(root, newGraph);
            watch.Stop();
            
            TLog.Debug($"Graph creation time: {watch.ElapsedMilliseconds}");
            NetGraphResolver.ResolveGraph(newGraph, root);
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
                    foreach (var output in part.CellIO.OuterConnnectionCells)
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
                    InternalEdgeSearch(directPart, directPos, rootNode.CellIO.OuterModeFor(directPos), rootNode);
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
                        foreach (var output in part.CellIO.OuterConnnectionCells)
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

        //Root Searcher
        private static INetworkSubPart FindNextNodeAlongEdge(INetworkSubPart rootEdge)
        {
            //
            var currentSubSet = StaticListHolder<INetworkSubPart>.RequestList("CurrentSubSet");
            var openSubSet = StaticListHolder<INetworkSubPart>.RequestList("OpenSubSet");
            var closedSubSot = StaticListHolder<INetworkSubPart>.RequestList("ClosedSubSet");
            
            //
            openSubSet.Add(rootEdge);
            while (openSubSet.Count > 0)
            {
                //Swap Current with Open
                (currentSubSet, openSubSet) = (openSubSet, currentSubSet);
                openSubSet.Clear();
                foreach (var part in currentSubSet)
                {
                    foreach (var output in part.CellIO.OuterConnnectionCells)
                    {
                        List<Thing> thingList = output.GetThingList(rootEdge.Parent.Thing.Map);
                        foreach (var thing in thingList)
                        {
                            if (!Fits(thing, part.NetworkDef, out var newPart)) continue;
                            if (closedSubSot.Contains(newPart)) continue;
                            if (newPart.ConnectsTo(part))
                            {
                                //If Node - Quit
                                if (newPart.IsNetworkNode)
                                {
                                    currentSubSet.Clear();
                                    openSubSet.Clear();
                                    closedSubSot.Clear();
                                    return newPart;
                                }
                                //If Edge, continue search
                                openSubSet.Add(newPart); 
                                closedSubSot.Add(newPart);
                                break;
                            }
                        }
                    }
                }
            }
            currentSubSet.Clear();
            openSubSet.Clear();
            closedSubSot.Clear();
            return null;
        }
        
        
        private static void InitGraphSearch(INetworkSubPart searchRoot, NetworkGraph forGraph, INetworkSubPart nodeToIgnore = null)
        {
            AddNetworkData(searchRoot, forGraph);
            SplitSearch(searchRoot, forGraph, nodeToIgnore, null, searchRoot);
        }
        
        private static void AddNetworkData(INetworkSubPart newPart, NetworkGraph forGraph)
        {
            _ClosedGlobalSet.Add(newPart);
            newPart.Network = forGraph.ParentNetwork;
            forGraph.ParentNetwork.Notify_AddPart(newPart);
        }

        private static void SplitSearch(INetworkSubPart searchRoot, NetworkGraph forGraph, INetworkSubPart nodeToIgnore = null, int? splitOffLength = null, INetworkSubPart splitParent = null)
        {
            Action searchEvent = (() => {});
            foreach (var cell in searchRoot.CellIO.OuterConnnectionCells)
            {
                var newSearchRoot = GetFittingPartAt(searchRoot, cell, forGraph.ParentNetwork.ParentManager.Map);
                if (newSearchRoot == null) continue;
                
                //If node directly available, then add and split from new node
                if (newSearchRoot.IsNetworkNode)
                {
                    //TLog.Message($"Trying to set direct conn from {searchRoot.Parent.Thing} to {newSearchRoot.Parent.Thing}", TColor.Purple);
                    var newEdge = new NetEdge(searchRoot, newSearchRoot);
                    if (TrySetEdge(newEdge, forGraph))
                    {
                        searchEvent += () => 
                        {
                            InitGraphSearch(newSearchRoot, forGraph, searchRoot);
                        };
                        continue;
                    }
                }
                searchEvent += () =>
                {
                    TryGraphNodeSearch(newSearchRoot, forGraph, nodeToIgnore, splitOffLength, splitParent);
                };
            }
            searchEvent.Invoke();
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

        private static void TryGraphNodeSearch(INetworkSubPart searchNode, NetworkGraph forGraph, INetworkSubPart nodeToIgnore = null, int? splitOffLength = null, INetworkSubPart splitParent = null)
        {
            TLog.Message($"Starting Graph Node Search: '{searchNode}' PrevL: {splitOffLength}".Colorize(Color.magenta));
            int edgeLength = splitOffLength ?? 1;
            INetworkSubPart edgeParent = splitParent ?? searchNode;
            IntVec3 startCell = edgeParent.Parent.Thing.Position;

            //
            _CurrentSubSet.Clear();
            _OpenSubSet.Clear();
            _OpenSubSet.Add(searchNode);

            while (_OpenSubSet.Count > 0)
            {
                //Continue Search Until Next Node
                foreach (INetworkSubPart part in _OpenSubSet)
                {
                    AddNetworkData(part, forGraph);
                }

                //Swap Current with Open
                (_CurrentSubSet, _OpenSubSet) = (_OpenSubSet, _CurrentSubSet);
                _OpenSubSet.Clear();
                foreach (INetworkSubPart part in _CurrentSubSet)
                {
                    foreach (var output in part.CellIO.OuterConnnectionCells)
                    {
                        List<Thing> thingList = output.GetThingList(forGraph.ParentNetwork.ParentManager.Map);
                        foreach (var thing in thingList)
                        {
                            if (!Fits(thing, part.NetworkDef, out INetworkSubPart newPart)) continue;
                            if(!newPart.ConnectsTo(part)) continue;
                            if (newPart == nodeToIgnore) continue;
                            if (newPart == edgeParent) continue;

                            var newEdge = new NetEdge();//new NetEdge(edgeParent, newPart, startCell, part.Parent.Thing.Position, edgeLength);
                            if (_ClosedGlobalSet.Contains(newPart))
                            {
                                if (newPart.IsNetworkNode)
                                {
                                    if (TrySetEdge(newEdge, forGraph))
                                    {
                                        
                                    }
                                    continue;
                                }
                                else continue;
                            }

                            if (newPart.ConnectsTo(part))
                            {
                                //If Junction, split
                                /*
                                if (newPart.IsJunction)
                                {
                                    _CurrentSubSet.Clear();
                                    _OpenSubSet.Clear();

                                    TLog.Message($"Found {"Junction".Colorize(Color.green)}: '{newPart.Parent.Thing}' - Splitting");
                                    AddNetworkData(newPart, forGraph);
                                    SplitSearch(newPart, forGraph, searchNode, edgeLength, edgeParent);
                                    return;
                                }
                                */

                                //If Node - Quit current search
                                if (newPart.IsNetworkNode)
                                {
                                    _CurrentSubSet.Clear();
                                    _OpenSubSet.Clear();
                                    TLog.Message($"Found {"Node".Colorize(Color.cyan)}: '{newPart.Parent.Thing}' - Setting Edge [{edgeParent.Parent.Thing} -- {newPart.Parent.Thing}]");

                                    //Found Node for edge, create edge and start search for more nodes
                                    if (newEdge.endNode != null && newEdge.endNode != edgeParent)
                                    {
                                        if (TrySetEdge(newEdge, forGraph))
                                        {
                                            //if(isPreviousNodeNeedConn) continue;
                                            InitGraphSearch(newPart, forGraph, edgeParent);
                                        }
                                    }
                                    return;
                                }

                                //If Edge, continue search
                                _OpenSubSet.Add(newPart);
                                _ClosedGlobalSet.Add(newPart);
                                edgeLength++;
                                break;
                            }
                        }
                    }
                }
            }

            _CurrentSubSet.Clear();
            _OpenSubSet.Clear();
        }

        internal static INetworkSubPart GetFittingPartAt(IntVec3 c, Map map, NetworkDef forDef)
        {
            if (!c.IsValid) return null;
            List<Thing> thingList = c.GetThingList(map);

            //Result and Path Search
            foreach (var thing in thingList)
            {
                //Check Compatibility
                if (!Fits(thing, forDef, out INetworkSubPart newNodeResult))
                {
                    continue;
                }
                return newNodeResult;
            }
            return null;
        }

        //Helper Methods
        internal static INetworkSubPart GetFittingPartAt(INetworkSubPart forNode, IntVec3 c, Map map, INetworkSubPart excludeNode = null, bool useClosedSet = false)
        {
            List<Thing> thingList = c.GetThingList(map);

            //Result and Path Search
            foreach (var thing in thingList)
            {
                //Check Compatibility
                if (!Fits(thing, forNode.NetworkDef, out INetworkSubPart newNodeResult))
                {
                    continue;
                }

                //
                if (useClosedSet && _ClosedGlobalSet.Contains(newNodeResult)) continue;

                //Select new node as result
                if (newNodeResult != excludeNode && newNodeResult.ConnectsTo(forNode, out _, out _))
                {
                    return newNodeResult;
                }
            }
            return null;
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
