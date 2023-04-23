using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OneOf.Types;
using TMPro;
using UnityEngine;
using Verse;

namespace TeleCore
{
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
                    case "I":
                        Inner = ParseIOMode(mode);
                        break;
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
    }

    /// <summary>
    /// An IntVec3 with a relative direction attached.
    /// </summary>
    public readonly struct IntVec3Rot
    {
        private readonly Rot4 direction;
        private readonly IntVec3 vec;

        //public static implicit operator IntVec3(IntVec3Rot vec) => vec.vec;
        public static implicit operator IntVec3(IntVec3Rot vec) => vec.vec;
        public static implicit operator IntVec3Rot(IntVec3 vec) => new (vec, Rot4.Invalid);

        public Rot4 Direction => direction;
        public IntVec3 IntVec => vec;

        public IntVec3Rot(IntVec3 vec, Rot4 direction)
        {
            this.direction = direction;
            this.vec = vec;
        }

        public override string ToString()
        {
            return $"{vec}[{direction}]";
        }

        public override bool Equals(object obj)
        {
            if (obj is IntVec3 otherVec)
            {
                return vec.Equals(otherVec);
            }
            return base.Equals(obj);
        }
    }

    public struct RenderIOCell
    {
        public NetworkIOMode mode;
        public IntVec3Rot pos;
        
        public RenderIOCell(IntVec3Rot pos, NetworkIOMode mode)
        {
            this.pos = pos;
            this.mode = mode;
        }
    }

    public class NetworkCellIOSimple
    {
        private readonly Dictionary<Rot4, RenderIOCell[]> _cellyByRot;

        public NetworkCellIOSimple(string pattern, ThingDef def)
        {
            _cellyByRot = new Dictionary<Rot4, RenderIOCell[]>();
            for (int i = 0; i < 4; i++)
            {
                Rot4 rot = new Rot4(i);
                var rotatedSize = RotatedSize(rot, def.size);
                var rect = new CellRect(0 - rotatedSize.x / 2, 0 - rotatedSize.z / 2, rotatedSize.x, rotatedSize.z).ToArray();
                var cells = NetworkCellIO.RotateIOCells(NetworkCellIO.GetIOArr(NetworkCellIO.DefaultFallbackIfNecessary(pattern, rotatedSize)), rot, def.size);

                var newCells = new List<RenderIOCell>(); //new RenderIOCell[cells.Length*4];
                for (var c = 0; c < cells.Length; c++)
                {
                    var cell = rect[c];
                    var ioCell = cells[c];
                    var cellNorth = cell + GenAdj.CardinalDirections[0];
                    var cellEast = cell + GenAdj.CardinalDirections[1];
                    var cellSouth = cell + GenAdj.CardinalDirections[2];
                    var cellWest = cell + GenAdj.CardinalDirections[3];

                    if (!rect.Contains(cellNorth))
                    {
                        newCells.Add(new RenderIOCell(new IntVec3Rot(cellNorth, Rot4.North), ioCell.North));
                    }

                    if (!rect.Contains(cellEast))
                    {
                        newCells.Add(new RenderIOCell(new IntVec3Rot(cellEast, Rot4.East), ioCell.East));
                    }

                    if (!rect.Contains(cellSouth))
                    {
                        newCells.Add(new RenderIOCell(new IntVec3Rot(cellSouth, Rot4.South), ioCell.South));
                    }

                    if (!rect.Contains(cellWest))
                    {
                        newCells.Add(new RenderIOCell(new IntVec3Rot(cellWest, Rot4.West), ioCell.West));
                    }
                }
                _cellyByRot.Add(rot, newCells.ToArray());
            }
        }

        private static IntVec2 RotatedSize(Rot4 rotation, IntVec2 size)
        {
            return !rotation.IsHorizontal ? size : new IntVec2(size.z, size.x);
        }

        public void Draw(IntVec3 center, ThingDef def, Rot4 rot)
        {
            if (_cellyByRot.TryGetValue(rot, out var cells))
            {
                foreach (var renderIOCell in cells)
                {
                    var cell = center + renderIOCell.pos;
                    var drawPos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);

                    switch (renderIOCell.mode)
                    {
                        case NetworkIOMode.Input:
                            Graphics.DrawMesh(MeshPool.plane10, drawPos, (renderIOCell.pos.Direction.AsAngle - 180).ToQuat(), TeleContent.IOArrow, 0);
                            break;
                        case NetworkIOMode.Output:
                            Graphics.DrawMesh(MeshPool.plane10, drawPos, renderIOCell.pos.Direction.AsQuat, TeleContent.IOArrow, 0);
                            break;
                        case NetworkIOMode.TwoWay:
                            Graphics.DrawMesh(MeshPool.plane10, drawPos, renderIOCell.pos.Direction.AsQuat, TeleContent.IOArrowTwoWay, 0); 
                            break;
                    }
                }
            }
        }
    }

    public class NetworkCellIO
    {
        internal const char _Input = 'I';
        internal const char _Output = 'O';
        internal const char _TwoWay = '+';
        internal const char _Empty = '#';
        internal const char _Visual = '=';
        
        internal const string regexPattern = @"\[[^\]]*\]|.";
        
        //
        private Dictionary<NetworkIOMode, IntVec3Rot[]> _innerCells;
        private Dictionary<NetworkIOMode, IntVec3Rot[]> _outerCells;
        private Dictionary<IntVec3, (bool isOuter, NetworkIOMode mode)> _modeByCell;

        public IntVec3[] InnerConnnectionCells { get; private set; }
        public IntVec3Rot[] OuterConnnectionCells { get; private set; }
        public IntVec3[] VisualConnectionCells { get; private set; }

        public NetworkCellIO(string pattern, Thing thing)
        {
            _innerCells = new Dictionary<NetworkIOMode, IntVec3Rot[]>();
            _outerCells = new Dictionary<NetworkIOMode, IntVec3Rot[]>();
            _modeByCell = new Dictionary<IntVec3, (bool isOuter, NetworkIOMode mode)>();

            //
            GenerateFromPattern(pattern, thing);
        }
        
        public NetworkIOMode IOModeFor(IntVec3 pos)
        {
            return _modeByCell.GetValueOrDefault(pos).mode;
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
                        _modeByCell.Add(cell, (false, ioCell.Inner));
                        visualCells.Add(cell);
                    }

                    var cellNorth = cell + GenAdj.CardinalDirections[0];
                    var cellEast = cell + GenAdj.CardinalDirections[1];
                    var cellSouth = cell + GenAdj.CardinalDirections[2];
                    var cellWest = cell + GenAdj.CardinalDirections[3];
                    
                    if (!rect.Contains(cellNorth))
                    {
                        AddIOCell(_outerCells, ioCell.North, new IntVec3Rot(cellNorth, Rot4.North));
                        _modeByCell.Add(cellNorth, (true, ioCell.North));
                    }

                    if (!rect.Contains(cellEast))
                    {
                        AddIOCell(_outerCells, ioCell.East, new IntVec3Rot(cellEast, Rot4.East));
                        _modeByCell.Add(cellEast, (true, ioCell.East));
                    }

                    if (!rect.Contains(cellSouth))
                    {
                        AddIOCell(_outerCells, ioCell.South, new IntVec3Rot(cellSouth, Rot4.South));
                        _modeByCell.Add(cellSouth, (true, ioCell.South));
                    }

                    if (!rect.Contains(cellWest))
                    {
                        AddIOCell(_outerCells, ioCell.West, new IntVec3Rot(cellWest, Rot4.West));
                        _modeByCell.Add(cellWest, (true, ioCell.West));
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

        public bool ConnectsTo(NetworkCellIO otherGeneralIO, out IntVec3 intersectingCell, out NetworkIOMode IOMode)
        {
            intersectingCell = IntVec3.Invalid;
            IOMode = NetworkIOMode.None;

            for (var i = 0; i < OuterConnnectionCells.Length; i++)
            {
                var outerCell = OuterConnnectionCells[i];
                var reverse = outerCell.IntVec + outerCell.Direction.Opposite.FacingCell;
                
                if (SubMatch(outerCell, otherGeneralIO, out var mode) && otherGeneralIO.SubMatch(reverse, this, out var mode2))
                {
                    intersectingCell = outerCell;
                    IOMode = mode;
                    if (mode != mode2)
                    {
                        TLog.Warning($"Modes did not match up on connection check {mode} != {mode2}! On {intersectingCell}");
                    }

                    return true;
                }
            }
            return false;
        }

        private bool SubMatch(IntVec3 cell, NetworkCellIO other, out NetworkIOMode modeResult)
        {
            var outerMode = _modeByCell.GetValueOrDefault(cell, (false, NetworkIOMode.None));
            var innerMode = other._modeByCell.GetValueOrDefault(cell, (false, NetworkIOMode.None));

            modeResult = outerMode.Item2;
            if (outerMode.Item1 == innerMode.Item1) return false;
            return Matches(outerMode.Item2, innerMode.Item2);
        }
        
        public static bool Matches(NetworkIOMode innerMode, NetworkIOMode outerMode)
        {
            var innerInput = (innerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
            var outerInput = (outerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
            
            var innerOutput = (innerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
            var outerOutput = (outerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
            
            return (innerInput && outerOutput) || (outerInput && innerOutput);
        }
        
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
        
        #endregion
    }
}
