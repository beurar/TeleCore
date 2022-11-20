using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
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

            //Search For Initial Node
            if (root.IsNetworkEdge)
            {
                TLog.Message($"Root is edge, searching for new root node...");
                root = FindNextNodeAlongEdge(root);
                TLog.Message($"New Root Node: {root}");

                if (root == null)
                {
                    TLog.Warning("New root is null.");
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

        //Special search for next best node
        private static INetworkSubPart FindNextNodeAlongEdge(INetworkSubPart rootEdge)
        {
            _CurrentSubSet.Clear();
            _OpenSubSet.Clear();
            _OpenSubSet.Add(rootEdge);
            while (_OpenSubSet.Count > 0)
            {
                //Swap Current with Open
                (_CurrentSubSet, _OpenSubSet) = (_OpenSubSet, _CurrentSubSet);
                _OpenSubSet.Clear();
                foreach (INetworkSubPart part in _CurrentSubSet)
                {
                    TLog.Debug($"Output Cells: {part.CellIO.OuterConnnectionCells.Length}");
                    foreach (var output in part.CellIO.OuterConnnectionCells)
                    {
                        List<Thing> thingList = output.GetThingList(rootEdge.Parent.Thing.Map);
                        foreach (var thing in thingList)
                        {
                            if (!Fits(thing, part.NetworkDef, out INetworkSubPart newPart)) continue;
                            if (_ClosedGlobalSet.Contains(newPart)) continue;
                            if (newPart.ConnectsTo(part))
                            {
                                //If Node - Quit
                                if (newPart.IsNetworkNode)
                                {
                                    _CurrentSubSet.Clear();
                                    _OpenSubSet.Clear();
                                    _ClosedGlobalSet.Clear();
                                    return newPart;
                                }
                                //If Edge, continue search
                                _OpenSubSet.Add(newPart); 
                                _ClosedGlobalSet.Add(newPart);
                                break;
                            }
                        }
                    }
                }
            }
            _CurrentSubSet.Clear();
            _OpenSubSet.Clear();
            _ClosedGlobalSet.Clear();
            return null;
        }

        private static bool TrySetEdge(NetEdge newEdge, NetworkGraph forGraph)
        {
            //Add Edge To Graph with found nodes
            if (forGraph.AddEdge(newEdge))
            {
                newEdge.fromNode.Notify_SetConnection(newEdge.toNode, newEdge);
                newEdge.toNode.Notify_SetConnection(newEdge.fromNode, newEdge);
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
                            if(!newPart.CellIO.ConnectsTo(part.CellIO)) continue;
                            if (newPart == nodeToIgnore) continue;
                            if (newPart == edgeParent) continue;

                            var newEdge = new NetEdge(edgeParent, newPart, startCell, part.Parent.Thing.Position, edgeLength);
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
                                    if (newEdge.toNode != null && newEdge.toNode != edgeParent)
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
        internal static INetworkSubPart GetFittingPartAt(INetworkSubPart forNode, IntVec3 c, Map map, INetworkSubPart excludeNode = null)
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
                if (_ClosedGlobalSet.Contains(newNodeResult)) continue;

                //Select new node as result
                if (newNodeResult != excludeNode && newNodeResult.ConnectsTo(forNode))
                {
                    return newNodeResult;
                }
            }
            return null;
        }

        //Check whether or not a thing is part of a network
        internal static bool Fits(Thing thing, NetworkDef forNetwork, out INetworkSubPart part)
        {
            Comp_NetworkStructure structure = (Comp_NetworkStructure) (thing as ThingWithComps)?.AllComps.Find(t => t is Comp_NetworkStructure);
            part = structure?[forNetwork];
            return part != null;
        }
    }
}
