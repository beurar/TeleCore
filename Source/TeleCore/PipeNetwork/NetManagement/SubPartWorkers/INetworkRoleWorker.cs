using TeleCore.FlowCore;
using TeleCore.Network;

namespace TeleCore;

public interface INetworkRoleWorker
{
    //Network Getters
    public NetworkContainer Container { get; }
    public NetworkSubPartProperties Props { get; }
}