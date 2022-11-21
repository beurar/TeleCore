using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    /// <summary>
    /// This class manages all adjacent <see cref="NetworkSubPart"/> nodes of a specific NetworkComponent within a <see cref="PipeNetwork"/> graph. Especially for directional edges.
    /// </summary>
    public class AdjacentNodePartSet
    {
        private INetworkSubPart parentNode;
        private HashSet<NetEdge> allEdges;
        private Dictionary<IntVec3Rot, NetEdge> outgoingEdges;
        private Dictionary<IntVec3Rot, NetEdge> incomingEdges;

        public int EdgeCount => outgoingEdges.Count;
        public int IncomingEdgeCount => incomingEdges.Count;

        public AdjacentNodePartSet(NetworkSubPart parent)
        {
            this.parentNode = parent;
            outgoingEdges = new Dictionary<IntVec3Rot, NetEdge>();
            incomingEdges = new Dictionary<IntVec3Rot, NetEdge>();
        }

        public void Notify_ParentDestroyed()
        {

        }

        public void Notify_Clear()
        {
            allEdges.Clear();
            outgoingEdges.Clear();
            incomingEdges.Clear();
        }

        public void Notify_SetEdge(NetEdge edge, IntVec3Rot ioCell)
        {
            allEdges.Add(edge);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Outgoing: ");
            foreach (var edge in allEdges)
            {
                sb.AppendLine(edge.ToStringSimple(parentNode));
            }

            /*
            sb.AppendLine("Incoming: ");
            foreach (var edge in incomingEdges)
            {
                sb.AppendLine(edge.Value.ToString(parentNode));
            }
            */

            return sb.ToString();
        }
    }
}
