using TeleCore.FlowCore;

namespace TeleCore
{
    public interface IContainerLeaker
    {
        bool ShouldLeak { get; }
        NetworkContainer Container { get; }
    }
}
