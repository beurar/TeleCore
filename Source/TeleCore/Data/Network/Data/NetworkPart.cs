using System;
using System.Collections.Generic;
using TeleCore.Network.Flow;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore.Network.Data;

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
        set => _networkIO = value;
    }

    public NetworkPartSet AdjacentSet => _adjacentSet;
    
    public NetworkVolume Volume => Network.FlowSystem.Relations[this];

    public bool IsController => (Config.roles | NetworkRole.Controller) == NetworkRole.Controller;

    public bool IsEdge => Config.roles == NetworkRole.Transmitter;
    public bool IsNode => !IsEdge;

    public bool IsJunction { get; }
    public bool IsWorking { get; }
    public bool IsReceiving { get; }
    public bool HasContainer { get; }
    public bool HasConnection { get; }
    public bool IsLeaking { get; }

    public void PostDestroy(DestroyMode mode, Map map)
    {
    }

    public void Tick()
    {
    }

    public IOConnectionResult HasIOConnectionTo(INetworkPart other)
    {
        if (other == this) return IOConnectionResult.Invalid;
        if (!Config.networkDef.Equals(other.Config.networkDef)) return IOConnectionResult.Invalid;
        if (!Parent.CanConnectToOther(other.Parent)) return IOConnectionResult.Invalid;
        return PartIO.ConnectsTo(other.PartIO);
    }

    public void Draw()
    {
        //TODO: Draw stuff
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

        foreach (var g in Network.GetGizmos()) yield return g;
    }

    public void PartSetup(bool respawningAfterLoad)
    {
    }

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
            PartIO = new NetworkIO(config.netIOConfig, parent.Thing.Position, parent.Thing.Rotation);
    }

    #endregion
}