using Verse;

namespace TeleCore;

public interface IContainerHolder<T> where T : FlowValueDef
{
    string ContainerTitle { get; }
    ContainerProperties ContainerProps { get; }
    BaseContainer<T> Container { get; }

    void Notify_ContainerFull();
    void Notify_ContainerStateChanged();
    void Notify_AddedContainerValue(T def, float value);
}