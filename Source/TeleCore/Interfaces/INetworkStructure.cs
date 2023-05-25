using System.Collections.Generic;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore;

public interface INetworkStructure
{
    //Data References
    public Thing Thing { get; }
    public List<NetworkSubPart> NetworkParts { get; }
    public NetworkCellIO GeneralIO { get; }

    //States
    public bool IsPowered { get; }
    public bool IsWorking { get; }
        
    //
    void NetworkPartProcessorTick(INetworkSubPart subPart);
    void NetworkPostTick(NetworkSubPart networkSubPart, bool isPowered);

    //
    void Notify_ReceivedValue();

    //Methods
    void Notify_StructureAdded(INetworkStructure other);
    void Notify_StructureRemoved(INetworkStructure other);

    //
    bool RoleIsActive(NetworkRole role);
    bool AcceptsValue(NetworkValueDef value);
    bool CanInteractWith(INetworkSubPart other);
    bool CanConnectToOther(INetworkStructure other);
}