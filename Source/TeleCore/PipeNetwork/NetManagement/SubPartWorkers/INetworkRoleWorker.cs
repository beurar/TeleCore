using TeleCore.FlowCore;

namespace TeleCore;

public interface INetworkRoleWorker
{
    //Network Getters
    public PipeNetwork Network { get; }
    INetworkSubPart Part { get; set; }
    public NetworkContainer Container { get; }
    public NetworkSubPartProperties Props { get; }
}