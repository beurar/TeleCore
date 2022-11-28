using Verse;

namespace TeleCore;

public interface IContainerHolderThing<T> : IContainerHolder<T> where T : FlowValueDef
{
    Thing Thing { get; }
}