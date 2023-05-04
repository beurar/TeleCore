using Verse;

namespace TeleCore.Data.Network.Generation;

internal struct DelayedNetworkAction
{
    public DelayedNetworkActionType type;
    public NetworkSubPart subPart;
    public IntVec3 pos;

    public DelayedNetworkAction(DelayedNetworkActionType type, NetworkSubPart subPart, IntVec3 pos)
    {
        this.type = type;
        this.subPart = subPart;
        this.pos = pos;
    }
}