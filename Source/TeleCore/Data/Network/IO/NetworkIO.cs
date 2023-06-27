using System.Collections.Generic;
using TeleCore.Network.Utility;
using Verse;

namespace TeleCore.Network.IO;

/// <summary>
/// 
/// </summary>
public class NetworkIO
{
    private List<IOCell> _cells;
    private List<IOCell> _visCells;
    
    public List<IOCell> Connections => _cells;
    public List<IOCell> VisualCells => _visCells;
    
    public NetworkIO(NetIOConfig config, IntVec3 refPos, Rot4 parentRotation)
    {
        _cells = new List<IOCell>();
        _visCells = new List<IOCell>();
        
        var cells = config.GetCellsFor(parentRotation);
        foreach (var cell in cells)
        {
            if(cell.mode == NetworkIOMode.None) continue;
            if ((NetworkIOMode.ForRender & cell.mode) != 0)
            {
                _visCells.Add(new IOCell()
                {
                    Pos = new(cell.offset + refPos, cell.direction),
                    Mode = cell.mode
                });
            }

            _cells.Add(new IOCell()
            {
                Pos = new(cell.offset + refPos, cell.direction),
                Mode = cell.mode
            });
        }
    }

    public NetworkIOMode IOModeAt(IntVec3 pos)
    {
        foreach (var cell in _cells)
            if (cell.Pos.Pos == pos)
                return cell.Mode;
        return 0;
    }

    public IOConnectionResult ConnectsTo(NetworkIO other)
    {
        foreach (var cell in _cells)
        {
            foreach (var otherCell in other._cells)
            {
                if (cell.Pos != otherCell.Interface) continue;
                
                if (cell.Mode.Matches(otherCell.Mode))
                    return new IOConnectionResult()
                    {
                        In = cell.Pos,
                        Out = cell.Pos + cell.Pos.Dir.Opposite.FacingCell,
                        InMode = cell.Mode,
                        OutMode = otherCell.Mode
                    };
                else if (otherCell.Mode.Matches(cell.Mode)) //Reverse
                    return new IOConnectionResult()
                    {
                        In = otherCell.Pos,
                        Out = otherCell.Pos + otherCell.Pos.Dir.Opposite.FacingCell,
                        InMode = otherCell.Mode,
                        OutMode = cell.Mode
                    };
            }
        }

        return IOConnectionResult.Invalid;
    }
}