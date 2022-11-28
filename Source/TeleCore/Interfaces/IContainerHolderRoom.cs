using Verse;

namespace TeleCore;

public interface IContainerHolderRoom<T> : IContainerHolder<T> where T : FlowValueDef
{
    public RoomComponent Room { get; }
}