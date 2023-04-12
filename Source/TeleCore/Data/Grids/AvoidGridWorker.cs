using TeleCore.Data.Events;
using Verse;

namespace TeleCore;

public class AvoidGridWorker
{
    protected Map map;
    protected AvoidGridDef def;
    protected byte[] grid;

    public byte[] Grid => grid;
    
    public AvoidGridWorker(Map map, AvoidGridDef def)
    {
        this.map = map;
        this.def = def;
        grid = new byte[map.cellIndices.NumGridCells];
    }

    public virtual bool AffectsThing(Thing thing)
    {
        return true;
    }
    
    public virtual void Notify_CellChanged(CellChangedEventArgs args)
    {
        
    }

    protected void SetAvoidValue(int index, byte value)
    {
        grid[index] = value;
    }

    protected void SetAvoidValue(IntVec3 cell, byte value)
    {
        grid[map.cellIndices.CellToIndex(cell)] = value;
    }
}