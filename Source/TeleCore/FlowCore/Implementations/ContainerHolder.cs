using TeleCore;
using Verse;

namespace TeleCore.Network;

//Base holder provides settings for the container
public interface IContainerHolderBase<TValue>
    where TValue : FlowValueDef
{
    public string ContainerTitle { get; }

    void Notify_ContainerStateChanged(NotifyContainerChangedArgs<TValue> args);
}

/// <summary>
/// Container Implementation extension which allows you to expose a <see cref="Thing"/> reference
/// </summary>
public interface IContainerHolderThing<TValue> : IContainerHolderBase<TValue>
    where TValue : FlowValueDef
{
    public Thing Thing { get; }
    public bool ShowStorageGizmo { get; }
}

/// <summary>
/// Implements a container for a <see cref="Room"/>
/// </summary>
public interface IContainerHolderRoom<TValue> : IContainerHolderBase<TValue>
    where TValue : FlowValueDef
{
    public Room Room { get; }
    public RoomComponent RoomComponent { get; }
}