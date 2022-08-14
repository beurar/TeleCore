using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    public interface INetworkStructure
    {
        //Data References
        public Thing Thing { get; }
        public List<NetworkSubPart> NetworkParts { get; }
        public NetworkCellIO GeneralIO { get; }

        //Internal Data
        public bool IsPowered { get; }

        //
        void NetworkPartProcessor(INetworkSubPart subPart);
        void NetworkPostTick(bool isPowered);

        //
        void Notify_ReceivedValue();

        //Methods
        void Notify_StructureAdded(INetworkStructure other);
        void Notify_StructureRemoved(INetworkStructure other);

        //
        bool AcceptsValue(NetworkValueDef value);
        bool CanConnectToOther(INetworkStructure other);
    }
}
