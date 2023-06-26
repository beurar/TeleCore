using System;
using System.Collections.Generic;
using TeleCore.Defs;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using TeleCore.Network.IO;
using TeleCore.Primitive;
using Verse;

namespace TeleCore.Network.Data;

public interface INetworkPart
{
    //General Data
    public NetworkPartConfig Config { get; }
    public INetworkStructure Parent { get; }
    public Thing Thing { get; }
    public PipeNetwork Network { get; internal set; }
    public NetworkIO NetworkIO { get; }
    public NetworkPartSet AdjacentSet { get; }
    public FlowBox FlowBox { get; }
    //public NetworkPartSet DirectPartSet { get; }
    
    //States
    public bool IsController { get; }
    public bool IsNode { get; }
    public bool IsEdge { get; }
    public bool IsJunction { get; }

    public bool Working { get; }
    
    [Obsolete("This has been basically replaced by the FlowSystem, if prevflow has a value it means there was value transfer")]
    public bool IsReceiving { get; }

    public bool HasContainer { get; }
    public bool HasConnection { get; }
    public bool IsLeaking { get; }

    public void PostDestroy(DestroyMode mode, Map map);
    
    void Tick();
    
    IOConnectionResult HasIOConnectionTo(INetworkPart otherPart);

    public void Draw();

    public string InspectString();

    /*
    void Notify_ReceivedValue();
    void Notify_StateChanged(string signal);

    void Notify_SetConnection(NetEdge edge, IntVec3Rot ioCell);
    void Notify_NetworkDestroyed();


    bool CanInteractWith(INetworkPart other);
    IOConnectionResult ConnectsTo(INetworkPart otherPart);
    bool CanTransmit(NetEdge netEdge);
    bool NeedsValue(NetworkValueDef value, NetworkRole forRole);
    */
    IEnumerable<Gizmo> GetPartGizmos();
}