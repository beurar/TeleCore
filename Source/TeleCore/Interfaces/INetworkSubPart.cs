
namespace TeleCore
{
    public interface INetworkSubPart
    {
        //General Data
        public NetworkSubPartProperties Props { get; }
        public NetworkDef NetworkDef { get; }
        public NetworkRole NetworkRole { get; }
        public INetworkStructure Parent { get; }
        public NetworkCellIO CellIO { get; }
        public PipeNetwork Network { get; set; }
        public NetworkPartSet DirectPartSet { get; }
        public AdjacentNodePartSet AdjacencySet { get; }
        public NetworkContainer Container { get; }

        //States
        public bool IsMainController { get; }
        public bool IsNetworkNode { get; }
        public bool IsNetworkEdge { get; }
        public bool IsJunction { get; }

        public bool CanWork { get; }
        public bool IsReceiving { get; }

        public bool HasContainer { get; }
        public bool HasConnection { get; }
        public bool HasLeak { get; }

        //Updates
        void NetworkTick();

        void Notify_ReceivedValue();
        void Notify_StateChanged(string signal);

        void Notify_SetConnection(NetEdge edge, IntVec3Rot ioCell);
        void Notify_NetworkDestroyed();

        bool ConnectsTo(INetworkSubPart otherPart);
        bool CanTransmit(NetEdge netEdge);
        bool NeedsValue(NetworkValueDef value, NetworkRole forRole);
    }
}
