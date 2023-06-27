using System.Collections.Generic;
using TeleCore.Defs;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore.Network.Data;

public class NetworkPart : INetworkPart
{
    private NetworkPartConfig _config;
    private INetworkStructure _parent;
    private PipeNetwork _network;
    private NetworkIO _networkIO;
    private NetworkPartSet _adjSet;

    public NetworkPartConfig Config => _config;
    public INetworkStructure Parent => _parent;
    public Thing Thing => _parent.Thing;
    
    public PipeNetwork Network
    {
        get => _network;
        set => _network = value;
    }

    public NetworkIO NetworkIO => _networkIO;
    public NetworkPartSet AdjacentSet => _adjSet;
    public FlowBox FlowBox => _network.FlowSystem.Relations[this];

    public bool IsController => (_config.role | NetworkRole.Controller) == NetworkRole.Controller;
    
    public bool IsEdge => _config.role == NetworkRole.Transmitter;
    public bool IsNode => !IsEdge;
    
    public bool IsJunction { get; }
    public bool IsWorking { get; }
    public bool IsReceiving { get; }
    public bool HasContainer { get; }
    public bool HasConnection { get; }
    public bool IsLeaking { get; }

    #region Constructors

    public NetworkPart(){}
        
    public NetworkPart(INetworkStructure parent)
    {
        _parent = parent;
    }
        
    public NetworkPart(INetworkStructure parent, NetworkPartConfig config) : this(parent)
    {
        _config = config;

        if(config.netIOConfig != null)
            _networkIO = new NetworkIO(config.netIOConfig, parent.Thing.Position, parent.Thing);
    }

    #endregion
    
    public void PartSetup(bool respawningAfterLoad)
    {
        
    }
    
    public void PostDestroy(DestroyMode mode, Map map)
    {
        
    }

    public void Tick()
    {
        
    }

    public IOConnectionResult HasIOConnectionTo(INetworkPart other)
    {
        if (other == this) return IOConnectionResult.Invalid;
        if (!_config.networkDef.Equals(other.Config.networkDef)) return IOConnectionResult.Invalid;
        if (!Parent.CanConnectToOther(other.Parent)) return IOConnectionResult.Invalid;
        return _networkIO.ConnectsTo(other.NetworkIO);
    }

    public void Draw()
    {
        throw new System.NotImplementedException();
    }

    public string InspectString()
    {
        throw new System.NotImplementedException();
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

        foreach (var g in _network.GetGizmos())
        {
            yield return g;
        }
    }
}