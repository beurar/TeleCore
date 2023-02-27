using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Verse;

namespace TeleCore;

//TODO:Reduce generics to one level <TValue> : FlowValueDef
//TODO:Make inherited Holder chain base > thing > network
//TODO:                                 > room
//

public interface IContainerHolderBase
{
    
}

/// <summary>
/// Implements a <see cref="BaseContainer{TValue}"/> as a generic universal 
/// </summary>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TContainer"></typeparam>
public interface IContainerHolderUniversal<TValue, out TContainer> : IContainerHolderBase
    where TValue : FlowValueDef
    where TContainer : BaseContainer<TValue>
{
    //Properties
    public string ContainerTitle { get; }
    public ContainerProperties ContainerProperties { get; }
    public TContainer Container { get; }
    
    //Methods
    void Notify_ContainerStateChanged(NotifyContainerChangedArgs<TValue> args);
}


/// <summary>
/// Container Implementation extension which allows you to expose a <see cref="Thing"/> reference
/// </summary>
public interface IContainerHolderThing<TValue, out TContainer> : IContainerHolderUniversal<TValue, TContainer>
    where TValue : FlowValueDef
    where TContainer : BaseContainer<TValue>
{
    public Thing Thing { get; }
    public bool ShowStorageForThingGizmo { get; }
}

/// <summary>
/// Implements a container for a <see cref="Room"/>
/// </summary>
public interface IContainerHolderRoom<TValue, out TContainer> : IContainerHolderUniversal<TValue, TContainer>
    where TValue : FlowValueDef
    where TContainer : BaseContainer<TValue>
{
    public RoomComponent RoomComponent { get; }
}

public struct ContainerArgs
{
    public string Name { get; }
    public int Capactiy { get; }
    public bool StoreEvenly { get; }
}

public enum NotifyContainerChangedAction
{
    AddedValue,
    RemovedValue,
    Filled,
    Emptied
}

public class NotifyContainerChangedArgs<TValue> : EventArgs
where TValue : FlowValueDef
{
    public NotifyContainerChangedAction Action { get; }
    public DefValueStack<TValue> ValueDelta { get; }

    public NotifyContainerChangedArgs(DefValueStack<TValue> delta, DefValueStack<TValue> final)
    {
        ValueDelta = delta;

        //Resolve Action
        Action = delta > 0 ? NotifyContainerChangedAction.AddedValue : NotifyContainerChangedAction.RemovedValue;
        if (final.Empty && delta < 0)
            Action = NotifyContainerChangedAction.Emptied;
        if ((final.Full ?? false) && delta > 0)
            Action = NotifyContainerChangedAction.Filled;
    }
}

//Network
public interface IContainerHolderNetworkBase<THolder, TContainer> : IContainerHolderThing<NetworkValueDef, THolder, TContainer>
    where THolder : IContainerHolderThing<NetworkValueDef, THolder, TContainer>
    where TContainer : ContainerForThing<NetworkValueDef, THolder, TContainer>
{
    
}

public interface IContainerHolderNetworkThing : IContainerHolderNetworkBase<IContainerHolderNetworkThing, NetworkContainerThing>
{
    
}

public interface IContainerHolderNetwork : IContainerHolderNetworkBase<IContainerHolderNetwork, NetworkContainer>
{
    INetworkSubPart NetworkPart { get; }
    NetworkContainerSet ContainerSet { get; }
}

/*public class ClassA<TInterface, TClass> 
    where TInterface : IInterfaceA<TInterface, TClass>
    where TClass : ClassA<TInterface, TClass>
{
    public TInterface Interface { get; }
}

public interface IInterfaceA<TInterface, TClass> 
    where TInterface : IInterfaceA<TInterface, TClass>
    where TClass : ClassA<TInterface, TClass>
{
    public TClass Class { get; }
}*/