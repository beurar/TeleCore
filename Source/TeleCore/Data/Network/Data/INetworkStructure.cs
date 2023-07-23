using System.Collections.Generic;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore.Network;

public interface INetworkStructure
{
    //Data References
    public Thing Thing { get; }

    public List<NetworkPart> NetworkParts { get; }
    public NetworkIO GeneralIO { get; }

    //States
    public bool IsPowered { get; }
    public bool IsWorking { get; }

    //
    void NetworkPostTick(INetworkPart networkSubPart, bool isPowered);

    //
    void Notify_ReceivedValue();

    //Methods
    void Notify_StructureAdded(INetworkStructure other);
    void Notify_StructureRemoved(INetworkStructure other);

    //
    bool RoleIsActive(NetworkRole role);
    bool AcceptsValue(NetworkValueDef value);
    bool CanInteractWith(INetworkPart other);
    bool CanConnectToOther(INetworkStructure other);
}