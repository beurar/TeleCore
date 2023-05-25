using TeleCore;

namespace TeleCore.Network;

//Implementer - glues both the holder and the container together and exposes a Container Property
public interface IContainerImplementer<TValue, THolder, out TContainer>
    where TValue : FlowValueDef
    where THolder : IContainerHolderBase<TValue>
    where TContainer : ValueContainer<TValue, THolder>
{
    public TContainer Container { get; }
}