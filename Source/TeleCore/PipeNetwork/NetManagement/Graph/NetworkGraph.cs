using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class NetworkGraphRequestManager
    {
        private readonly NetworkGraph parent;

        //Caching
        private readonly Dictionary<NetworkGraphNodeRequest, NetworkGraphRequestResult> _cachedRequestResults;
        private readonly Dictionary<INetworkSubPart, List<NetworkGraphNodeRequest>> _nodesOnCachedResult;
        private readonly HashSet<NetworkGraphNodeRequest> _dirtyRequests;

        public NetworkGraphRequestManager(NetworkGraph graph)
        {
            parent = graph;
            _cachedRequestResults = new();
            _nodesOnCachedResult = new();
            _dirtyRequests = new();
        }

        public void Notify_NodeStateChanged(INetworkSubPart part)
        {
            if (_nodesOnCachedResult.TryGetValue(part, out var requests))
            {
                _dirtyRequests.AddRange(requests);
            }
        }

        private void CheckRequestDirty(NetworkGraphNodeRequest request)
        {
            if (_dirtyRequests.Contains(request))
            {
                TLog.Debug("Request is dirty.. removing");
                //If request has been cached
                if (_cachedRequestResults.TryGetValue(request, out var cachedResult))
                {
                    //Remove request from all nodes associated
                    foreach (var var in cachedResult.allPartsUnique)
                    {
                        var list = _nodesOnCachedResult[var];
                        list.Remove(request);
                        
                        
                        //
                        if (list.Count == 0)
                        {
                            TLog.Debug($"Clearing last request binding from {var}");
                            _nodesOnCachedResult.Remove(var);
                        }
                    }

                    _cachedRequestResults.Remove(request);
                    _dirtyRequests.Remove(request);
                }
            }
        }

        private NetworkGraphRequestResult CreateAndCacheRequest(NetworkGraphNodeRequest request)
        {
            TLog.Debug($"Processing new request...\n{request}");

            List<List<INetworkSubPart>> result = GenGraph.Dijkstra(parent, request);
            var requestResult = new NetworkGraphRequestResult(request, result);
            _cachedRequestResults.Add(request, requestResult);

            //
            foreach (var part in requestResult.allPartsUnique)
            {
                if (!_nodesOnCachedResult.TryGetValue(part, out var list))
                {
                    list = new List<NetworkGraphNodeRequest>() { request };
                    _nodesOnCachedResult.Add(part, list);
                }
                list.Add(request);
            }

            return requestResult;
        }

        public NetworkGraphRequestResult ProcessRequest(NetworkGraphNodeRequest request)
        {
            //Check dirty result
            CheckRequestDirty(request);

            //Get existing result
            if (_cachedRequestResults.TryGetValue(request, out var value))
            {
                TLog.Debug("Found cached request... returning");
                return value;
            }

            //
            return CreateAndCacheRequest(request);
        }
    }

    public class NetworkGraph
    {
        private NetworkGraphRequestManager _requestManager;

        //Graph Data
        private readonly List<INetworkSubPart> _allNodes;
        private readonly Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>> _adjacencyLists;
        private readonly Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge> _edges;

        //Props
        public int NodeCount => _adjacencyLists.Count;
        public int EdgeCount => _edges.Count;

        public List<INetworkSubPart> AllNodes => _allNodes;
        public Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>> AdjacencyLists => _adjacencyLists;

        public PipeNetwork ParentNetwork { get; internal set; }

        public NetworkGraph()
        {
            _allNodes = new List<INetworkSubPart>();
            _adjacencyLists = new Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>>();
            _edges = new Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge>();

            _requestManager = new NetworkGraphRequestManager(this);
        }

        public void Notify_StateChanged(INetworkSubPart part)
        {
            _requestManager.Notify_NodeStateChanged(part);
        }

        public NetworkGraphRequestResult ProcessRequest(NetworkGraphNodeRequest request)
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
            listSource?.AddFirst(newEdge.toNode);
            listSource2?.AddFirst(newEdge.fromNode);
            return true;
        }

        //
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

        //
        public void DrawGraphOnUI()
        {
            foreach (var netEdge in _edges)
            {
                var subParts = netEdge.Key;
                var thingA = subParts.Item1.Parent.Thing;
                var thingB = subParts.Item2.Parent.Thing;

                Widgets.DrawLine(ScreenPositionOf(thingA.TrueCenter()), ScreenPositionOf(thingB.TrueCenter()), Color.red, 2);

                DrawBoxOnThing(thingA);
                DrawBoxOnThing(thingB);
            }
        }

        public void DrawBoxOnThing(Thing thing)
        {
            var v = ScreenPositionOf(thing.TrueCenter());
            var rect = new Rect(v.x-25, v.y-25, 50, 50);
            TWidgets.DrawColoredBox(rect, new Color(1, 1, 1, 0.125f), Color.white, 1);
        }

        private Vector2 ScreenPositionOf(Vector3 vec)
        {
            Vector2 vector = Find.Camera.WorldToScreenPoint(vec) / Prefs.UIScale;
            vector.y = (float)UI.screenHeight - vector.y;
            return vector;
        }
    }
}
