using Verse;

namespace TeleCore;

public interface IContainerHolderRoom : IContainerHolder
{
    public RoomComponent Room { get; }
}