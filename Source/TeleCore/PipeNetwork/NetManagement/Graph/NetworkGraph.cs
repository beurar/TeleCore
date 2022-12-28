using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class NetworkGraph
    {
        private NetworkGraphRequestManager _requestManager;

        //Graph Data
        private readonly List<INetworkSubPart> _allNodes;
        private readonly Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>> _adjacencyLists;
        private readonly Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge> _edges;
        //private readonly HashSet<(NetEdge, NetEdge)> _edgePairs;

        //Props
        public int NodeCount => _adjacencyLists.Count;
        public int EdgeCount => _edges.Count;

        public List<INetworkSubPart> AllNodes => _allNodes;
        private Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>> AdjacencyLists => _adjacencyLists;

        public PipeNetwork ParentNetwork { get; internal set; }

        internal NetworkGraphRequestManager Requester => _requestManager;

        public NetworkGraph()
        {
            _allNodes = new List<INetworkSubPart>();
            _adjacencyLists = new Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>>();
            _edges = new Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge>();
            //_edgePairs = new HashSet<(NetEdge, NetEdge)>();

            _requestManager = new NetworkGraphRequestManager(this);
        }

        public void Notify_StateChanged(INetworkSubPart part)
        {
            _requestManager.Notify_NodeStateChanged(part);
        }

        public NetworkGraphPathResult ProcessRequest(NetworkGraphPathRequest request)
        {
            return _requestManager.ProcessRequest(request);
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

        public void AddNode(INetworkSubPart node)
        {
            _allNodes.Add(node);
            _adjacencyLists.Add(node, new LinkedList<INetworkSubPart>());
        }

        public bool AddEdge(NetEdge newEdge)
        {
            var newKey = (fromNode: newEdge.startNode, toNode: newEdge.endNode);
            var reverseKey = (newEdge.endNode, newEdge.startNode);
            if (_edges.ContainsKey(newKey) || _edges.ContainsKey(reverseKey))
            {
                TLog.Warning($"Key ({newEdge.startNode.Parent.Thing}, {newEdge.endNode.Parent.Thing}) already exists in graph!");
                return false;
            }

            _edges.Add(newKey, newEdge);
            
            /*
            if (_edges.TryGetValue((newEdge.toNode, newEdge.fromNode), out var value))
            {
                TLog.Message("Adding edge pair");
                _edgePairs.Add((newEdge, value));
            }
            */
            
            if (!_adjacencyLists.TryGetValue(newEdge.startNode, out var listSource))
            {
                AddNode(newEdge.startNode);
                listSource = _adjacencyLists[newEdge.startNode];
            }
            if (!_adjacencyLists.TryGetValue(newEdge.endNode, out var listSource2))
            {
                AddNode(newEdge.endNode);
                listSource2 = _adjacencyLists[newEdge.endNode];
            }
            
            if(!listSource.Contains(newEdge.endNode))
                listSource.AddFirst(newEdge.endNode);
            else
                TLog.Warning($"Already added {newEdge.endNode}");
            
            if(!listSource2.Contains(newEdge.startNode))
                listSource2?.AddFirst(newEdge.startNode);
            else
                TLog.Warning($"Already added {newEdge.startNode}");
            return true;
        }

        //
        public bool HasKnownEdgeFor(INetworkSubPart fromRoot, IntVec3 cell, out NetEdge netEdge)
        {
            netEdge = new NetEdge();
            return false;
            //fromRoot.AdjacencySet
        }

        public bool GetAnyEdgeBetween(INetworkSubPart source, INetworkSubPart dest, out NetEdge value)
        {
            value = GetEdgeFor(source, dest, true);
            return value.IsValid;
        }
        
        public bool TryGetEdge(INetworkSubPart source, INetworkSubPart dest, out NetEdge value)
        {
            value = GetEdgeFor(source, dest);
            return value.IsValid;
            return _edges.TryGetValue((source, dest), out value);// || _edges.TryGetValue((dest, source), out value);
        }

        private NetEdge GetEdgeFor(INetworkSubPart source, INetworkSubPart dest, bool any = false)
        {
            if (_edges.TryGetValue((source, dest), out var value))
            {
                return value;
            }

            if (_edges.TryGetValue((dest, source), out value))
            {
                return any ? value : value.Reverse;
            }

            return NetEdge.Invalid;
        }
        
        public IEnumerable<NetEdge> EdgesFor(INetworkSubPart startNode)
        {
            foreach (var adjacentNode in _adjacencyLists.TryGetValue(startNode))
            {
                if (_edges.TryGetValue((startNode, adjacentNode), out var edge))
                    yield return edge;
            }
        }

        //Debug stuff
        private static readonly Material FilledMat = SolidColorMaterials.NewSolidColorMaterial(Color.green, ShaderDatabase.MetaOverlay);
        private static readonly Material UnFilledMat = SolidColorMaterials.NewSolidColorMaterial(TColor.LightBlack, ShaderDatabase.MetaOverlay);
        
        internal void Debug_DrawGraphOnUI()
        {
            var size = Find.CameraDriver.CellSizePixels / 4;
            /*
            foreach (var pair in _edgePairs)
            {
                var edge1 = pair.Item1;
                var edge2 = pair.Item2;
                
                if(edge1.IsValid)
                    TWidgets.DrawHalfArrow(edge1.fromNode.Parent.Thing.TrueCenter().ToScreenPos(), edge1.toNode.Parent.Thing.TrueCenter().ToScreenPos(), Color.red, size);
                if(edge2.IsValid)
                    TWidgets.DrawHalfArrow(edge2.fromNode.Parent.Thing.TrueCenter().ToScreenPos(), edge2.toNode.Parent.Thing.TrueCenter().ToScreenPos(), Color.blue, size);
            }
            */
            
            foreach (var netEdge in _edges)
            {
                var subParts = netEdge.Key;
                var thingA = subParts.Item1.Parent.Thing;
                var thingB = subParts.Item2.Parent.Thing;
                
                //TWidgets.DrawHalfArrow(ScreenPositionOf(thingA.TrueCenter()), ScreenPositionOf(thingB.TrueCenter()), Color.red, 8);

                //TODO: edge access only works for one version (node1, node2) - breaks two-way
                //TODO:some edges probably get setup broken (because only one edge is set)
                if (netEdge.Value.IsValid)
                {
                    TWidgets.DrawHalfArrow(netEdge.Value.startNode.Parent.Thing.TrueCenter().ToScreenPos(), netEdge.Value.endNode.Parent.Thing.TrueCenter().ToScreenPos(), Color.red, size);
                    if (netEdge.Value.IsBiDirectional)
                    {
                        TWidgets.DrawHalfArrow(netEdge.Value.endNode.Parent.Thing.TrueCenter().ToScreenPos(), netEdge.Value.startNode.Parent.Thing.TrueCenter().ToScreenPos(), Color.blue, size);  
                    }
                }
                TWidgets.DrawBoxOnThing(thingA);
                TWidgets.DrawBoxOnThing(thingB);
            }
        }

        public void Debug_DrawCachedResults()
        {
            _requestManager.Debug_DrawCachedResults();
        }
        
        internal void Debug_DrawPressure()
        {
            foreach (var networkSubPart in AllNodes)
            {
                if(!networkSubPart.HasContainer) continue;
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = networkSubPart.Parent.Thing.Position.ToVector3() + new Vector3(0.25f, 0, 0.75f);
                r.size = new Vector2(1.5f, 0.5f);
                r.fillPercent = networkSubPart.Container.StoredPercent;
                r.filledMat = FilledMat;
                r.unfilledMat = UnFilledMat;
                r.margin = 0f;
                r.rotation = Rot4.East;
                GenDraw.DrawFillableBar(r);
            }
        }

        public void Debug_DrawOverlays()
        {
            foreach (var networkSubPart in AllNodes)
            {
                var pos = networkSubPart.Parent.Thing.DrawPos;
                GenMapUI.DrawText(new Vector2(pos.x, pos.z), $"[{networkSubPart.Parent.Thing}]", Color.green);
            }
        }
    }
}
