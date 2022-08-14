using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    [Flags]
    public enum NetworkIOMode : byte
    {
        Input = 1,
        Output = 2,
        None = 3,
        TwoWay = Input & Output
    }

    public struct GraphNodeSearchPath
    {
        public readonly INetworkSubPart startNode;
        public readonly IntVec3 startCell;

        public readonly HashSet<IntVec3> knownCells;

        public INetworkSubPart lastNode;
        public IntVec3 lastCell;
        public INetworkSubPart foundNode;

        public GraphNodeSearchPath(INetworkSubPart node, IntVec3 cell)
        {
            startNode = node;
            startCell = cell;
            knownCells = new HashSet<IntVec3>();
            foundNode = null;

            //
            lastNode = node;
            lastCell = cell;
        }

        public bool IsValid => startNode != null;

        public void AddCell(IntVec3 cell, INetworkSubPart ofNode)
        {
            lastNode = ofNode;
            lastCell = cell;
            knownCells.Add(cell);
        }

        public void Finish(INetworkSubPart foundNode)
        {
            this.foundNode = foundNode;
        }
    }

    public struct NetEdge
    {
        internal readonly int _weight;

        //Direction
        public readonly INetworkSubPart fromNode;
        public readonly INetworkSubPart toNode;
        public readonly IntVec3 fromCell;
        public readonly IntVec3 toCell;

        public NetEdge Reverse => new(toNode, fromNode, toCell, fromCell, _weight);
        public static NetEdge Invalid => new(null, null, IntVec3.Invalid, IntVec3.Invalid, -1);

        public NetEdge(INetworkSubPart fromNode, INetworkSubPart toNode, IntVec3 fromCell, IntVec3 toCell, int weight)
        {
            this.fromNode = fromNode;
            this.toNode = toNode;
            this.fromCell = fromCell;
            this.toCell = toCell;
            this._weight = weight;
        }

        public bool HasAnchorCell(IntVec3 cell)
        {
            return fromCell == cell || toCell == cell;
        }

        public string ToStringSimple(INetworkSubPart node)
        {
            return $"{($"{fromNode.Parent.Thing}".Colorize(node == fromNode ? Color.cyan : Color.white))} -> {($"{toNode.Parent.Thing}".Colorize(node == toNode ? Color.cyan : Color.white))}";
        }

        public string ToString(INetworkSubPart node)
        {
            return $"{($"{fromNode.Parent.Thing}".Colorize(node == fromNode ? Color.cyan : Color.white))} -> {($"{toNode.Parent.Thing}".Colorize(node == toNode ? Color.cyan : Color.white))}| ({fromCell},{toCell})";
        }
    }

    public class NetworkGraph
    {
        //Graph Data
        private readonly Dictionary<INetworkSubPart, LinkedList<INetworkSubPart>> _adjacencyLists;
        private readonly Dictionary<(INetworkSubPart, INetworkSubPart), NetEdge> _edges;
        
        //
        public int NodeCount => _adjacencyLists.Count;
        public int EdgeCount => _edges.Count;

        public PipeNetwork ParentNetwork { get; internal set; }

        public NetworkGraph()
        {
            _adjacencyLists = new();
            _edges = new();
        }

        public void AddNode(INetworkSubPart node)
        {
            _adjacencyLists.Add(node, new LinkedList<INetworkSubPart>());
        }

        public NetEdge AddEdge(INetworkSubPart source, INetworkSubPart dest, GraphNodeSearchPath searchPath)
        {
            var newEdge = new NetEdge(source, dest, searchPath.startCell, searchPath.lastCell, searchPath.knownCells.Count);
            AddEdge(newEdge);
            return newEdge;
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
            listSource.AddFirst(newEdge.toNode);
            return true;
        }

        //
        public IEnumerable<INetworkSubPart> GetAdjacentNodes(INetworkSubPart node)
        {
            return _adjacencyLists[node];
        }

        public bool TryGetEdge(INetworkSubPart source, INetworkSubPart dest, out NetEdge value)
        {
            return _edges.TryGetValue((source, dest), out value);
        }

        public void AddNodeFrom(INetworkSubPart comp)
        {
            AddNode(comp);
        }

        public IEnumerable<NetEdge> EdgesFor(INetworkSubPart startNode)
        {
            foreach (var adjacentNode in _adjacencyLists.TryGetValue(startNode))
            {
                if (_edges.TryGetValue((startNode, adjacentNode), out var edge))
                    yield return edge;
            }
        }

        public void DrawGraphOnUI()
        {
            foreach (var netEdge in _edges)
            {
                var subParts = netEdge.Key;
                var thingA = subParts.Item1.Parent.Thing;
                var thingB = subParts.Item2.Parent.Thing;

                Widgets.DrawLine(PointFromVector(thingA.TrueCenter()), PointFromVector(thingB.TrueCenter()), Color.red, 2);

                DrawBoxOnThing(thingA);
                DrawBoxOnThing(thingB);
            }
        }

        public void DrawBoxOnThing(Thing thing)
        {
            var v = PointFromVector(thing.TrueCenter());
            var rect = new Rect(v.x-25, v.y-25, 50, 50);
            TWidgets.DrawColoredBox(rect, new Color(1, 1, 1, 0.125f), Color.white, 1);
        }

        private Vector2 PointFromVector(Vector3 vec)
        {
            Vector2 vector = Find.Camera.WorldToScreenPoint(vec) / Prefs.UIScale;
            vector.y = (float)UI.screenHeight - vector.y;
            return vector;
        }
    }
}
