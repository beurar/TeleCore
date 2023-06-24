using System.Collections.Generic;
using TeleCore.Network.Data;
using TeleCore.Network.Utility;
using Verse;

namespace TeleCore.Network.IO;

/// <summary>
/// 
/// </summary>
public class NetworkIO
{
    private List<IOCell> _cells;
    
    public List<IOCell> Connections => _cells;
    
    public NetworkIO(IOConfig config, IntVec3 refPos)
    {
        _cells = new List<IOCell>();
        for (var i = 0; i < config.cells.Count; i++)
        {
            var cell = config.cells[i];
            _cells.Add(new IOCell()
            {
                pos = new(cell.offset + refPos, cell.direction),
                mode = cell.mode
            });
        }
    }

    public IOConnectionResult ConnectsTo(NetworkIO other)
    {
        foreach (var cell in _cells)
        {
            foreach (var otherCell in other._cells)
            {
                if (cell.pos == otherCell.Interface)
                {
                    if (cell.mode.Matches(otherCell.mode))
                    {
                        return new IOConnectionResult()
                        {
                            In = cell.pos,
                            Out = cell.pos + cell.pos.Dir.Opposite.FacingCell,
                            InMode = cell.mode,
                            OutMode = otherCell.mode
                        };
                    }
                    else if (otherCell.mode.Matches(cell.mode)) //Reverse
                    {
                        return new IOConnectionResult()
                        {
                            In = otherCell.pos,
                            Out = otherCell.pos + otherCell.pos.Dir.Opposite.FacingCell,
                            InMode = otherCell.mode,
                            OutMode = cell.mode

                        };
                    }
                }
            }
        }
        return IOConnectionResult.Invalid;
    }
}