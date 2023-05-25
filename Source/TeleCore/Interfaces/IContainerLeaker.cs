using TeleCore.Network;

namespace TeleCore
{
    public interface IContainerLeaker
    {
        bool ShouldLeak { get; }
        NetworkContainer Container { get; }
    }
}
