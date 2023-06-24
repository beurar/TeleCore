using System.Collections.Generic;
using TeleCore.Primitive;
using Verse;

namespace TeleCore.Network.IO;

public class NetworkCellIO
{/*
    internal const char _Input = 'I';
    internal const char _Output = 'O';
    internal const char _TwoWay = '+';
    internal const char _Empty = '#';
    internal const char _Visual = '=';
        
    internal const string regexPattern = @"\[[^\]]*\]|.";
        
    //
    private Dictionary<NetworkIOMode, IntVec3Rot[]> _innerCells;
    private Dictionary<NetworkIOMode, IntVec3Rot[]> _outerCells;
    private Dictionary<IntVec3, (bool isOuter, IOCell mode)> _modeByCell;

    public IntVec3[] InnerConnnectionCells { get; private set; }
    public IntVec3Rot[] OuterConnnectionCells { get; private set; }
    public IntVec3[] VisualConnectionCells { get; private set; }

    public NetworkCellIO(string pattern, Thing thing)
    {
        _innerCells = new Dictionary<NetworkIOMode, IntVec3Rot[]>();
        _outerCells = new Dictionary<NetworkIOMode, IntVec3Rot[]>();
        _modeByCell = new Dictionary<IntVec3, (bool isOuter, IOCell mode)>();

        //
        GenerateFromPattern(pattern, thing);
    }
        
    public NetworkIOMode IOModeFor(IntVec3 pos, IntVec3? requestPos = null)
    {
        if(requestPos == null)
            return _modeByCell.GetValueOrDefault(pos).mode.Inner;
        return _modeByCell.GetValueOrDefault(pos).mode.DirectionalMode(pos.Rot4Relative(requestPos.Value));
    }
        
    #region Pattern Gen

    private void AddIOCell(Dictionary<NetworkIOMode, IntVec3Rot[]> forDict, NetworkIOMode mode, IntVec3Rot cell)
    {
        // Try to retrieve the existing array for the mode
        if (forDict.TryGetValue(mode, out IntVec3Rot[] existingArr))
        {
            // If the array already exists, create a new array with enough space for the new cell
            IntVec3Rot[] newArr = new IntVec3Rot[existingArr.Length + 1];

            // Copy the existing cells into the new array
            Array.Copy(existingArr, newArr, existingArr.Length);

            // Add the new cell to the end of the new array
            newArr[existingArr.Length] = cell;

            // Replace the existing array with the new array in the dictionary
            forDict[mode] = newArr;
        }
        else
        {
            IntVec3Rot[] newArr = { cell };
            forDict.Add(mode, newArr);
        }
    }

    private void GenerateFromPattern(string ioPattern, Thing thing)
    {
        var rect = thing.OccupiedRect();
        var rectList = rect.ToArray();

        int width = thing.RotatedSize.x;
        int height = thing.RotatedSize.z;

        ioPattern = DefaultFallbackIfNecessary(ioPattern, thing.RotatedSize);
        IOCell[] arr = RotateIOCells(GetIOArr(ioPattern), thing.Rotation, thing.def.size);

        List<IntVec3> visualCells = new List<IntVec3>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int actualIndex = y * width + x;
                var ioCell = arr[actualIndex];
                var cell = rectList[actualIndex];
                    
                if (ioCell.Inner != NetworkIOMode.None)
                {
                    AddIOCell(_innerCells, ioCell.Inner, cell);
                    _modeByCell.Add(cell, (false, ioCell));
                    visualCells.Add(cell);
                }

                var cellNorth = cell + GenAdj.CardinalDirections[0];
                var cellEast = cell + GenAdj.CardinalDirections[1];
                var cellSouth = cell + GenAdj.CardinalDirections[2];
                var cellWest = cell + GenAdj.CardinalDirections[3];
                    
                if (!rect.Contains(cellNorth))
                {
                    AddIOCell(_outerCells, ioCell.North, new IntVec3Rot(cellNorth, Rot4.North));
                    _modeByCell.Add(cellNorth, (true, ioCell));
                }

                if (!rect.Contains(cellEast))
                {
                    AddIOCell(_outerCells, ioCell.East, new IntVec3Rot(cellEast, Rot4.East));
                    _modeByCell.Add(cellEast, (true, ioCell));
                }

                if (!rect.Contains(cellSouth))
                {
                    AddIOCell(_outerCells, ioCell.South, new IntVec3Rot(cellSouth, Rot4.South));
                    _modeByCell.Add(cellSouth, (true, ioCell));
                }

                if (!rect.Contains(cellWest))
                {
                    AddIOCell(_outerCells, ioCell.West, new IntVec3Rot(cellWest, Rot4.West));
                    _modeByCell.Add(cellWest, (true, ioCell));
                }
            }
        }

        //
        var innerCells = _innerCells.SelectMany(c => c.Value).ToArray();
        var outerCells = _outerCells.SelectMany(c => c.Value).ToArray();
        InnerConnnectionCells = new IntVec3[innerCells.Length];
        OuterConnnectionCells = new IntVec3Rot[outerCells.Length];
        VisualConnectionCells = visualCells.ToArray();

        for (var k = 0; k < innerCells.Length; k++)
        {
            InnerConnnectionCells[k] = innerCells[k];
        }

        for (var kk = 0; kk < outerCells.Length; kk++)
        {
            OuterConnnectionCells[kk] = outerCells[kk];
        }
    }

    /// <summary>
    /// Rotates the pattern array to match the rotation of the thing.
    /// </summary>
    internal static IOCell[] RotateIOCells(IOCell[] arr, Rot4 rotation, IntVec2 size)
    {
        int xWidth = size.x;
        int yHeight = size.z;
        if (rotation == Rot4.East)
        {
            arr = arr.RotateLeft(xWidth, yHeight);
        }
        if (rotation == Rot4.South)
        {
            arr = arr.FLipHorizontal(xWidth, yHeight);
        }
        if (rotation == Rot4.West)
        {
            arr = arr.RotateRight(xWidth, yHeight);
        }

        for (var c = 0; c < arr.Length; c++)
        {
            arr[c] = arr[c].Rotate(rotation);
        }

        return arr;
    }
        
    internal static IOCell[] GetIOArr(string input)
    {
        input = input.Replace("|", "");
        MatchCollection matches = Regex.Matches(input, regexPattern);
        var io = new IOCell[matches.Count];
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.Value.Length == 1)
            {
                io[i] = new IOCell(match.Value[0]);
            }
            else
            {
                io[i] = new IOCell(match.Value);
            }
        }
        return io;
    }

    internal static string DefaultFallbackIfNecessary(string pattern, IntVec2 size)
    {
        if (pattern != null) return pattern;

        int widthx = size.x;
        int heighty = size.z;

        var charArr = new char[widthx * heighty];
            
        for (int x = 0; x < widthx; x++)
        {
            for (int y = 0; y < heighty; y++)
            {
                charArr[x + (y * widthx)] = _TwoWay;
            }
        }
        return charArr.ArrayToString();
    }

    #endregion
    
    #region Matching

    public IOConnectionResult ConnectsTo(NetworkCellIO otherGeneralIO)
    {
        for (var i = 0; i < OuterConnnectionCells.Length; i++)
        {
            var outerCell = OuterConnnectionCells[i];
            var result1 = SubMatch(outerCell, otherGeneralIO);
            
            if (result1)
            {
                return result1;
            }
            
            // if (SubMatch(outerCell, otherGeneralIO, out var mode) && otherGeneralIO.SubMatch(innerCell, this, out var mode2))
            // {
            //     return new IOConnectionResult
            //     {
            //         In = innerCell,
            //         Out = outerCell,
            //         InMode = mode,
            //         OutMode = mode2
            //     };
            // }
        }
        return IOConnectionResult.Invalid;
    }

    private IOConnectionResult SubMatch(IntVec3Rot cell, NetworkCellIO other)
    {
        var outerIOCell = _modeByCell.GetValueOrDefault(cell, (false, new IOCell(_Empty)));
        var innerIOCell = other._modeByCell.GetValueOrDefault(cell, (false, new IOCell(_Empty)));

        //Outer cells cant connect to outer cells from another IO
        if (outerIOCell.Item1 == innerIOCell.Item1) return IOConnectionResult.Invalid;
        var outerMode = outerIOCell.Item2.DirectionalMode(cell.Direction);
        var innerMode = innerIOCell.Item2.DirectionalMode(cell.Reverse.Direction);;
        
        if (Matches(outerMode, innerMode))
        {
            return new IOConnectionResult()
            {
                In = cell,
                Out = cell + cell.Direction.Opposite.FacingCell,
                InMode = outerMode,
                OutMode = innerMode
            };
        }
        return IOConnectionResult.Invalid;
    }

    public static bool Matches(NetworkIOMode innerMode, NetworkIOMode outerMode)
    {
        var innerInput = (innerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
        var outerInput = (outerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
            
        var innerOutput = (innerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
        var outerOutput = (outerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
            
        return (innerInput && outerOutput) || (outerInput && innerOutput);
    }
        
    [Obsolete]
    public static bool MatchesFromTo(NetworkIOMode from, NetworkIOMode to)
    {
        var fromOutput = (from | NetworkIOMode.Output) == NetworkIOMode.Output;
        var toInput = (to | NetworkIOMode.Input) == NetworkIOMode.Input;

        return (toInput && fromOutput);
    }

    #endregion

    #region Rendering
    public void DrawIO()
    {
        if (_outerCells.TryGetValue(NetworkIOMode.TwoWay, out var twoway))
        {
            foreach (var cell in twoway)
            {
                var drawPos = cell.IntVec.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                Graphics.DrawMesh(MeshPool.plane10, drawPos, cell.Direction.AsQuat, TeleContent.IOArrowTwoWay, 0);
            }
        }

        if (_outerCells.TryGetValue(NetworkIOMode.Output, out var output))
        {
            foreach (var cell in output)
            {
                var drawPos = cell.IntVec.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                Graphics.DrawMesh(MeshPool.plane10, drawPos, cell.Direction.AsQuat, TeleContent.IOArrow, 0);
            }
        }

        if (_outerCells.TryGetValue(NetworkIOMode.Input, out var input))
        {
            foreach (var cell in input)
            {
                var drawPos = cell.IntVec.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                Graphics.DrawMesh(MeshPool.plane10, drawPos, (cell.Direction.AsAngle - 180).ToQuat(), TeleContent.IOArrow, 0);
            }
        }
    }
        
    public void PrintIO(SectionLayer layer)
    {
        if (_outerCells.TryGetValue(NetworkIOMode.TwoWay, out var twoway))
        {
            foreach (var cell in twoway)
            {
                var drawPos = cell.IntVec.ToVector3Shifted();
                TDrawing.PrintBasic(layer, drawPos, Vector2.one, TeleContent.IOArrowTwoWay, 0, false);
            }
        }

        if (_outerCells.TryGetValue(NetworkIOMode.Output, out var output))
        {
            foreach (var cell in output)
            {
                var drawPos = cell.IntVec.ToVector3Shifted();
                TDrawing.PrintBasic(layer, drawPos, Vector2.one, TeleContent.IOArrow, cell.Direction.AsAngle, false);
            }
        }

        if (_outerCells.TryGetValue(NetworkIOMode.Input, out var input))
        {
            foreach (var cell in input)
            {
                var drawPos = cell.IntVec.ToVector3Shifted();
                TDrawing.PrintBasic(layer, drawPos, Vector2.one, TeleContent.IOArrow, (cell.Direction.AsAngle - 180), false);
            }
        }
    }
        
    #endregion*/
}