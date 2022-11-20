using Verse;

namespace TeleCore;

public interface IContainerHolderThing : IContainerHolder
{
    Thing Thing { get; }
}