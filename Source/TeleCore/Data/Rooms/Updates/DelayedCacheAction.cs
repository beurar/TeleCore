using Verse;

namespace TeleCore.Rooms.Updates;

internal struct DelayedCacheAction
{
    public IntVec3 Cell { get; }
    public int Index { get; }

    public DelayedCacheAction(IntVec3 cell, int index)
    {
        Cell = cell;
        Index = index;
    }
}
