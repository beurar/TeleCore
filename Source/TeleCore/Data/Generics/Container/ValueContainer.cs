using TeleCore.Defs;

namespace TeleCore.Network;

//Container Template implementing IContainerHolder
public abstract class ValueContainer<TValue, THolder> : ValueContainerBase<TValue>
    where TValue : FlowValueDef
    where THolder : IContainerHolderBase<TValue>
{
    public THolder Holder { get; }

    protected ValueContainer(ContainerConfig<TValue> config, THolder holder) : base(config)
    {
        Holder = holder;
    }

    public override void Notify_ContainerStateChanged(NotifyContainerChangedArgs<TValue> stateChangeArgs)
    {
        Holder?.Notify_ContainerStateChanged(stateChangeArgs);
    }
}
