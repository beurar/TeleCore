using TeleCore.Defs;
using TeleCore.Network.Graph;
using TeleCore.Network.IO;
using TeleCore.Primitive;
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
    
    public PipeNetwork Network => _network;
    public NetworkIO NetworkIO => _networkIO;
    public NetworkPartSet AdjacentSet => _adjSet;

    public bool IsController => (_config.role | NetworkRole.Controller) == NetworkRole.Controller;
    
    public bool IsEdge => _config.role == NetworkRole.Transmitter;
    public bool IsNode => !IsEdge;
    
    public bool IsJunction { get; }
    public bool Working { get; }
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
    }

    #endregion
    
    public void Tick()
    {
        
    }
}