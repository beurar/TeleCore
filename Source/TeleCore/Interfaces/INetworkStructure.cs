using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    public interface INetworkStructure
    {
        //Data References
        public Thing Thing { get; }
        public List<NetworkComponent> NetworkParts { get; }

        //Internal Data
        public bool IsPowered { get; }

        public IntVec3[] InnerConnectionCells { get; }
        public IntVec3[] ConnectionCells { get; }

        //Methods
        void Notify_StructureAdded(INetworkStructure other);
        void Notify_StructureRemoved(INetworkStructure other);
    }
}
