using System;
using System.Collections.Generic;
using System.Diagnostics;
using TeleCore.Network.Flow;
using TeleCore.Network.IO;
using TeleCore.Network.Utility;
using Verse;

namespace TeleCore.Network.Data;

[DebuggerDisplay("{Thing}")]
public class NetworkPart : INetworkPart
{
    private NetworkPartConfig _config;
    private INetworkStructure _parent;
    private PipeNetwork _network;
    private NetworkIO _networkIO;
    private NetworkPartSet _adjacentSet;
    
    public NetworkPartConfig Config
    {
        get => _config;
        set => _config = value;
    }

    public INetworkStructure Parent
    {
        get => _parent;
        set => _parent = value;
    }

    public Thing Thing => Parent.Thing;
    
    public PipeNetwork Network 
    { 
        get => _network;
        set => _network = value; 
    }
    
    public NetworkIO PartIO
    {
        get => _networkIO ?? Parent.GeneralIO;
    }

    public NetworkPartSet AdjacentSet => _adjacentSet;

    public NetworkVolume Volume => Network.NetworkSystem.Relations.TryGetValue(this) ?? null;

    public bool IsController => (Config.roles | NetworkRole.Controller) == NetworkRole.Controller;

    public bool IsEdge => Config.roles == NetworkRole.Transmitter;
    public bool IsNode => !IsEdge;
    public bool IsJunction => Config.roles == NetworkRole.Transmitter && _adjacentSet[NetworkRole.Transmitter]?.Count > 2;
    public bool HasConnection => _adjacentSet[NetworkRole.Transmitter]?.Count > 0;

    public bool IsWorking => true;
    public bool IsReceiving { get; }
    public bool HasContainer => Volume != null;
    public bool IsLeaking { get; }

    #region Constructors

    public NetworkPart()
    {
    }

    public NetworkPart(INetworkStructure parent)
    {
        Parent = parent;
    }

    //Main creation in Comp_Network with Activator.
    public NetworkPart(INetworkStructure parent, NetworkPartConfig config) : this(parent)
    {
        Config = config;
        _adjacentSet = new NetworkPartSet(config.networkDef);
        if (config.netIOConfig != null)
            _networkIO = new NetworkIO(config.netIOConfig, parent.Thing.Position, parent.Thing.Rotation);
    }

    #endregion
    
    public void PartSetup(bool respawningAfterLoad)
    {
        GetDirectlyAdjacentNetworkParts();
    }
    
    public void PostDestroy(DestroyMode mode, Map map)
    {
    }

    public void Tick()
    {
    }

    #region Helpers

    private void GetDirectlyAdjacentNetworkParts()
    {
        for (var c = 0; c < PartIO.Connections.Count; c++)
        {
            IntVec3 connectionCell = PartIO.Connections[c];
            List<Thing> thingList = connectionCell.GetThingList(Thing.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (PipeNetworkFactory.Fits(thingList[i], _config.networkDef, out var subPart))
                {
                    if (HasIOConnectionTo(subPart))
                    {
                        _adjacentSet.AddComponent(subPart);
                        subPart.AdjacentSet.AddComponent(this);
                    }
                }
            }
        }
    }

    #endregion

    public IOConnectionResult HasIOConnectionTo(INetworkPart other)
    {
        if (other == this) return IOConnectionResult.Invalid;
        if (!Config.networkDef.Equals(other.Config.networkDef)) 
            return IOConnectionResult.Invalid;
        if (!Parent.CanConnectToOther(other.Parent)) 
            return IOConnectionResult.Invalid;
        return PartIO.ConnectsTo(other.PartIO);
    }

    public string InspectString()
    {
        //TODO: re-add inspection
        return "";
    }

    public virtual IEnumerable<Gizmo> GetPartGizmos()
    {
        if (DebugSettings.godMode)
        {
            /*if (IsController)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "Show Entire Network",
                    action = delegate
                    {
                        DebugNetworkCells = !DebugNetworkCells;
                    }
                };
            }

            if (Network == null) yield break;

            yield return new Command_Action
            {
                defaultLabel = $"Draw Graph",
                action = delegate { Network.DrawInternalGraph = !Network.DrawInternalGraph; }
            };

            yield return new Command_Action
            {
                defaultLabel = $"Draw AdjacencyList",
                action = delegate { Network.DrawAdjacencyList = !Network.DrawAdjacencyList; }
            };


            yield return new Command_Action
            {
                defaultLabel = $"Draw FlowDirections",
                action = delegate { Debug_DrawFlowDir = !Debug_DrawFlowDir; }
            };*/
        }

        if(Network != null)
        {
            foreach (var g in Network.GetGizmos()) 
                yield return g;
        }
    }

    #region Rendering

    public void Draw()
    {
        //TODO: Draw stuff
    }
    
    public void Print(SectionLayer layer)
    {
        Config.networkDef.TransmitterGraphic?.Print(layer, Thing, 0, this);
    }

    #endregion
}