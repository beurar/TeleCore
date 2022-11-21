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
        private readonly HashSet<(NetEdge, NetEdge)> _edgePairs;

        //Props
        public int NodeCount => _adjacencyLists.Count;
        public int EdgeCount => _edges.Count;

        public List<INetworkSubPart> AllNodes => _allNodes;
        public Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>> AdjacencyLists => _adjacencyLists;

        public PipeNetwork ParentNetwork { get; internal set; }

        internal NetworkGraphRequestManager Requester => _requestManager;

        public NetworkGraph()
        {
            _allNodes = new List<INetworkSubPart>();
            _adjacencyLists = new Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>>();
            _edges = new Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge>();
            _edgePairs = new HashSet<(NetEdge, NetEdge)>();

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
        public void AddNode(INetworkSubPart node)
        {
            _allNodes.Add(node);
            _adjacencyLists.Add(node, new LinkedList<INetworkSubPart>());
        }

        public bool AddEdge(NetEdge newEdge)
        {
            var newKey = (newEdge.fromNode, newEdge.toNode);
            if (_edges.ContainsKey(newKey))
            {
                TLog.Warning($"Key ({newEdge.fromNode.Parent.Thing}, {newEdge.toNode.Parent.Thing}) already exists in graph!");
                return false;
            }
            
            _edges.Add(newKey, newEdge);
            if (_edges.TryGetValue((newEdge.toNode, newEdge.fromNode), out var value))
            {
                TLog.Message("Adding edge pair");
                _edgePairs.Add((newEdge, value));
            }
            
            if (!_adjacencyLists.TryGetValue(newEdge.fromNode, out var listSource))
            {
                AddNode(newEdge.fromNode);
                listSource = _adjacencyLists[newEdge.fromNode];
            }
            if (!_adjacencyLists.TryGetValue(newEdge.toNode, out var listSource2))
            {
                AddNode(newEdge.toNode);
                listSource2 = _adjacencyLists[newEdge.toNode];
            }
            
            if(!listSource.Contains(newEdge.toNode))
                listSource.AddFirst(newEdge.toNode);
            else
                TLog.Warning($"Already added {newEdge.toNode}");
            
            if(!listSource2.Contains(newEdge.fromNode))
                listSource2?.AddFirst(newEdge.fromNode);
            else
                TLog.Warning($"Already added {newEdge.fromNode}");
            return true;
        }

        //
        public bool HasKnownEdgeFor(INetworkSubPart fromRoot, IntVec3 cell, out NetEdge netEdge)
        {
            fromRoot.AdjacencySet
        }
        
        public bool TryGetEdge(INetworkSubPart source, INetworkSubPart dest, out NetEdge value)
        {
            return _edges.TryGetValue((source, dest), out value) || _edges.TryGetValue((dest, source), out value);
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
            foreach (var pair in _edgePairs)
            {
                var edge1 = pair.Item1;
                var edge2 = pair.Item2;
                TWidgets.DrawHalfArrow(edge1.fromNode.Parent.Thing.TrueCenter().ToScreenPos(), edge1.toNode.Parent.Thing.TrueCenter().ToScreenPos(), Color.red, size);
                TWidgets.DrawHalfArrow(edge2.fromNode.Parent.Thing.TrueCenter().ToScreenPos(), edge2.toNode.Parent.Thing.TrueCenter().ToScreenPos(), Color.blue, size);
            }
            
            foreach (var netEdge in _edges)
            {
                var subParts = netEdge.Key;
                var thingA = subParts.Item1.Parent.Thing;
                var thingB = subParts.Item2.Parent.Thing;
                
                //TWidgets.DrawHalfArrow(ScreenPositionOf(thingA.TrueCenter()), ScreenPositionOf(thingB.TrueCenter()), Color.red, 8);
                
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
    }
}
