using System;
using Verse;

namespace TeleCore.Network.IO;

//TODO: single-cell multi states need to ignore Inner, instead an accessor should get the correct mode based on the requesting direction
public struct IOCell
{
    public NetworkIOMode Inner = NetworkIOMode.None;
    public NetworkIOMode North = NetworkIOMode.None;
    public NetworkIOMode East = NetworkIOMode.None;
    public NetworkIOMode South = NetworkIOMode.None;
    public NetworkIOMode West = NetworkIOMode.None;

    private Rot4 curRot = Rot4.North;
    
    public IOCell(string cellString)
    {
        var parts = cellString.Trim('[', ']').Split(';');
        if (parts.Length <= 0) return;
        
        Inner = NetworkIOMode.Visual;
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (string.IsNullOrEmpty(trimmedPart))
                continue;

            var directionAndMode = trimmedPart.Split(':');
            if (directionAndMode.Length != 2)
            {
                TLog.Error($"Invalid IO cell part format: {part}");
            }

            var direction = directionAndMode[0];
            var mode = directionAndMode[1].ToUpper()[0];

            switch (direction.ToUpper())
            {
                case "N":
                    North = ParseIOMode(mode);
                    break;
                case "E":
                    East = ParseIOMode(mode);
                    break;
                case "S":
                    South = ParseIOMode(mode);
                    break;
                case "W":
                    West = ParseIOMode(mode);
                    break;
                default:
                    TLog.Error($"Invalid IO cell direction: {direction}");
                    break;
            }
        }
    }

    public IOCell(char singleMode)
    {
        Inner = singleMode switch
        {
            NetworkCellIO._Input => NetworkIOMode.Input,
            NetworkCellIO._Output => NetworkIOMode.Output,
            NetworkCellIO._TwoWay => NetworkIOMode.TwoWay,
            NetworkCellIO._Empty => NetworkIOMode.None,
            NetworkCellIO._Visual => NetworkIOMode.Visual,
            _ => throw new ArgumentException($"Invalid IO mode character: {singleMode}")
        };
        North = East = South = West = Inner;
    }

    private static NetworkIOMode ParseIOMode(char c)
    {
        return c switch
        {
            NetworkCellIO._Input => NetworkIOMode.Input,
            NetworkCellIO._Output => NetworkIOMode.Output,
            NetworkCellIO._TwoWay => NetworkIOMode.TwoWay,
            NetworkCellIO._Visual => NetworkIOMode.Visual,
            _ => NetworkIOMode.None
        };
    }

    public IOCell Rotate(Rot4 rotation)
    {
        if (curRot == rotation) return this;
            
        var pNorth = North;
        var pEast = East;
        var pSouth = South;
        var pWest = West;
            
        if (rotation == Rot4.North)
        {
            curRot = Rot4.North;
        }
        if (rotation == Rot4.East)
        {
            North = pWest;
            East = pNorth;
            South = pEast;
            West = pSouth;
            curRot = Rot4.East;
        }
        if (rotation == Rot4.South)
        {
            North = pSouth;
            East = pWest;
            South = pNorth;
            West = pEast;
            curRot = Rot4.South;
        }
        if (rotation == Rot4.West)
        {
            North = pEast;
            East = pSouth;
            South = pWest;
            West = pNorth;
            curRot = Rot4.West;
        }
        return this;
    }
    
    public NetworkIOMode DirectionalMode(Rot4 byRotation)
    {
        if (byRotation == Rot4.North)
            return North;
        if (byRotation == Rot4.East)
            return East;
        if (byRotation == Rot4.South)
            return South;
        if (byRotation == Rot4.West)
            return West;
        return Inner;
    }
}
