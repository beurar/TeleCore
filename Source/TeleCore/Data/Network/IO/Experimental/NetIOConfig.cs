using System.Collections.Generic;
using Verse;

namespace TeleCore.Network.IO.Experimental;

/// <summary>
/// The config for the IO cells around a network structure.
/// </summary>
public struct NetIOConfig
{
    public string pattern;
    public List<IOCellPrototype> cellsNorth;
    public List<IOCellPrototype> cellsEast;
    public List<IOCellPrototype> cellsSouth;
    public List<IOCellPrototype> cellsWest;

    public List<IOCellPrototype> CellsFor(Rot4 rot)
    {
        if (rot == Rot4.North)
            return cellsNorth;
        if (rot == Rot4.East)
            return cellsEast;
        if (rot == Rot4.South)
            return cellsSouth;

        return cellsWest;
    }

    public void PostLoad()
    {
        if (pattern != null)
        {
        }
    }
}