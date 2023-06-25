using System;
using TeleCore.Primitive;
using Verse;

namespace TeleCore.Network.IO.Experimental;

/// <summary>
/// Meant to be set and created in XML
/// </summary>
public struct IOCellPrototype
{
    public IntVec3 offset;
    public Rot4 direction;
    public NetworkIOMode mode;
}

/// <summary>
/// Implements the actual IO Cell of NetworkPart
/// </summary>
public struct IOCell
{
    public IntVec3Rot Pos { get; set; }
    public NetworkIOMode Mode { get; set; }

    public static implicit operator IntVec3(IOCell cell)
    {
        return cell.Pos.Pos;
    }

    public IntVec3 Interface => Pos.Pos + Pos.Dir.Opposite.FacingCell;
}