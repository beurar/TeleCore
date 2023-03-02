using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore;

//Implementer - glues both the holder and the container together and exposes a Container Property
public interface IContainerImplementer<TValue, THolder, out TContainer>
    where TValue : FlowValueDef
    where THolder : IContainerHolderBase<TValue>
    where TContainer : ValueContainer<TValue, THolder>
{
    public TContainer Container { get; }
}

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
    public RoomComponent RoomComponent { get; }
}

//Raw Implementation in a class, using all base types
public class ClassWithContainer : IContainerImplementer<FlowValueDef, IContainerHolderBase<FlowValueDef>, ValueContainer<FlowValueDef, IContainerHolderBase<FlowValueDef>>>
{
    public ValueContainer<FlowValueDef, IContainerHolderBase<FlowValueDef>> Container { get; }
    
    public void Foo()
    {
        var newContainer = new ClassWithContainer();
        if (FlowValueUtils.NeedsEqualizing(Container, newContainer.Container, out _, out _))
        {
            
        }
    }
}