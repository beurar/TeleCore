using Verse;

namespace TeleCore;

public interface IContainerHolder
{
    string ContainerTitle { get; }
    ContainerProperties ContainerProps { get; }
    NetworkContainer Container { get; }

    void Notify_ContainerFull();
    void Notify_ContainerStateChanged();
}