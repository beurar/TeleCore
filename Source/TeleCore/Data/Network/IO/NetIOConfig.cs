using System.Collections.Generic;
using TeleCore.Network.Utility;
using Verse;

namespace TeleCore.Network.IO;

/// <summary>
///     The config for the IO cells around a network structure.
///     <para>The IO modes are noted in<see cref="IOUtils"/></para>
/// </summary>
public class NetIOConfig : Editable
{
    public List<IOCellPrototype> cellsEast;
    public List<IOCellPrototype> cellsNorth;
    public List<IOCellPrototype> cellsSouth;
    public List<IOCellPrototype> cellsWest;

    public List<IOCellPrototype> cellsVisual;

    //
    public IntVec2? patternSize;
    public string pattern;

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

    public List<IOCellPrototype> GetCellsFor(Rot4 rotation)
    {
        if (rotation == Rot4.North)
            return cellsNorth;
        if (rotation == Rot4.East)
            return cellsEast;
        if (rotation == Rot4.South)
            return cellsSouth;

        return cellsWest;
    }

    private static void Rotate(List<IOCellPrototype> reference, ref List<IOCellPrototype> toRotate, Rot4 rot)
    {
        toRotate = new List<IOCellPrototype>();
        foreach (var cell in reference)
            toRotate.Add(new IOCellPrototype
            {
                offset = cell.offset.RotatedBy(Rot4.North),
                direction = Rot4.North,
                mode = cell.mode
            });
    }

    public void PostLoadCustom(ThingDef def)
    {
        if (patternSize == null && def != null)
            patternSize = def.size + new IntVec2(2, 2);
        
        if (pattern != null || (cellsNorth == null && cellsEast == null && cellsSouth == null & cellsWest == null)) 
            cellsNorth = IOUtils.GenerateFromPattern(pattern, patternSize.Value);

        var cells = cellsNorth ?? cellsEast ?? cellsSouth ?? cellsWest;

        if (cellsNorth == null) Rotate(cells, ref cellsNorth, Rot4.North);

        if (cellsEast == null) Rotate(cells, ref cellsEast, Rot4.East);

        if (cellsSouth == null) Rotate(cells, ref cellsSouth, Rot4.South);

        if (cellsWest == null) Rotate(cells, ref cellsWest, Rot4.West);
    }
}