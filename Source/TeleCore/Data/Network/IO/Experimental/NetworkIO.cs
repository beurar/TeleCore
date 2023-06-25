using System.Collections.Generic;
using Verse;

namespace TeleCore.Network.IO.Experimental;

/// <summary>
/// 
/// </summary>
public class NetworkIO
{
    private List<IOCell> _cells;

    public List<IOCell> Connections => _cells;

    //TODO: needs to account for null config and requires thing to set adjacent twoway
    public NetworkIO(NetIOConfig config, IntVec3 refPos)
    {
        _cells = new List<IOCell>();
        for (var i = 0; i < config.cells.Count; i++)
        {
            var cell = config.cells[i];
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
        foreach (var otherCell in other._cells)
            if (cell.Pos == otherCell.Interface)
            {
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

        return IOConnectionResult.Invalid;
    }
}