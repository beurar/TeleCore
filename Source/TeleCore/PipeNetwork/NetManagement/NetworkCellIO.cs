using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public readonly struct IntVec3Rot
    {
        private readonly Rot4 rot;
        private readonly IntVec3 vec;

        public static implicit operator IntVec3(IntVec3Rot vec) => vec.vec;
        public static implicit operator IntVec3Rot(IntVec3 vec) => new (vec, Rot4.Invalid);

        public Rot4 Rotation => rot;
        public IntVec3 IntVec => vec;

        public IntVec3Rot(IntVec3 vec, Rot4 rot)
        {
            this.rot = rot;
            this.vec = vec;
        }

        public override string ToString()
        {
            return $"{vec}[{rot.ToStringHuman()}]";
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

    public class NetworkCellIO
    {
        private const char _Input = 'I';
        private const char _Output = 'O';
        private const char _TwoWay = '+';
        private const char _Empty = '#';
        private const char _Visual = '=';
        
        //
        private static readonly string regexIgnore = @"[^IO+#]";
        private static readonly string regexMustHave = @"[IO+#|]";

        //
        private readonly Thing thing;
        private string connectionPattern;

        //
        private IntVec3[] cachedInnerConnectionCells;
        private IntVec3[] cachedConnectionCells;
        private IntVec3[] _cachedRenderCells;

        private Dictionary<char, IntVec3[]> InnerCellsByTag;
        private Dictionary<char, IntVec3Rot[]> OuterCellsByTag;

        public NetworkCellIO(string pattern, Thing thing)
        {
            this.connectionPattern = pattern;
            this.thing = thing;
            cachedInnerConnectionCells = null;
            cachedConnectionCells = null;
            _cachedRenderCells = null;
            InnerCellsByTag = new Dictionary<char, IntVec3[]>();
            OuterCellsByTag = new Dictionary<char, IntVec3Rot[]>();

            //
            GenerateIOCells();
        }

        public NetworkIOMode InnerModeFor(IntVec3 cell)
        {
            if (InnerCellsByTag.TryGetValue(_TwoWay, out var cells) && cells.Contains(cell)) return NetworkIOMode.TwoWay;
            if (InnerCellsByTag.TryGetValue(_Input, out cells) && cells.Contains(cell)) return NetworkIOMode.Input;
            if (InnerCellsByTag.TryGetValue(_Output, out cells) && cells.Contains(cell)) return NetworkIOMode.Output;
            return NetworkIOMode.None;
        }
        
        public NetworkIOMode OuterModeFor(IntVec3 cell)
        {
            if (OuterCellsByTag.TryGetValue(_TwoWay, out var cells) && cells.Any(c => c.IntVec == cell)) return NetworkIOMode.TwoWay;
            if (OuterCellsByTag.TryGetValue(_Input, out cells) && cells.Any(c => c.IntVec == cell)) return NetworkIOMode.Input;
            if (OuterCellsByTag.TryGetValue(_Output, out cells) && cells.Any(c => c.IntVec == cell)) return NetworkIOMode.Output;
            return NetworkIOMode.None;
        }

        public IntVec3[] InnerConnnectionCells => cachedInnerConnectionCells;
        public IntVec3[] OuterConnnectionCells => cachedConnectionCells;

        public IntVec3Rot[] InputCells => OuterCellsByTag.TryGetValue(_Input, Array.Empty<IntVec3Rot>());
        public IntVec3Rot[] OutputCells => OuterCellsByTag.TryGetValue(_Output, Array.Empty<IntVec3Rot>());
        public IntVec3Rot[] TwoWayCells => OuterCellsByTag.TryGetValue(_TwoWay, Array.Empty<IntVec3Rot>());

        private char CharForMode(NetworkIOMode mode)
        {
            switch (mode)
            {
                case NetworkIOMode.Input:
                    return _Input;
                case NetworkIOMode.Output:
                    return _Output;
                case NetworkIOMode.TwoWay:
                    return _TwoWay;
                default:
                    return ' ';
            }
        }
        
        private NetworkIOMode ModeFromChar(char c)
        {
            switch (c)
            {
                case _Input:
                    return NetworkIOMode.Input;
                case _Output:
                    return NetworkIOMode.Output;
                case _TwoWay:
                    return NetworkIOMode.TwoWay;
                default:
                    return NetworkIOMode.None;
            }
        }

        private void AddIOCell<TValue>(Dictionary<char, TValue[]> forDict, NetworkIOMode mode, TValue cell)
        {
            //Get Mode Key
            var modeChar = CharForMode(mode);

            //Adjust existing arrays
            TValue[] newArr = null;
            if (forDict.TryGetValue(modeChar, out var arr))
            {
                newArr = new TValue[arr.Length + 1];
                for (int i = 0; i < arr.Length; i++)
                {
                    newArr[i] = arr[i];
                }
                newArr[arr.Length] = cell;
            }
        
            //Add Mode Key if not existant
            if (!forDict.ContainsKey(modeChar))
                forDict.Add(modeChar, null);

            //Set new array
            newArr ??= new TValue[1] {cell};
            forDict[modeChar] = newArr;
        }

        //
        private void GenerateIOCells()
        {
            var pattern = GetCorrectPattern();
            var rect = thing.OccupiedRect();
            var rectList = rect.ToArray();
            var cellsInner = new List<IntVec3>();
            var cellsOuter = new List<IntVec3>();

            int width = thing.RotatedSize.x;
            int height = thing.RotatedSize.z;

            //TLog.Message($"Using Pattern: {pattern} from {connectionPattern}");
            //Inner Connection Cells
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int actualIndex = y * width + x;
                    //int inv = ((height - 1) - y) * width + x;

                    var c = pattern[actualIndex];
                    var cell = rectList[actualIndex];
                    
                    if (c != _Empty)
                    {
                        cellsInner.Add(cell);
                    }

                    if (c == _TwoWay)
                    {
                        AddIOCell(InnerCellsByTag, NetworkIOMode.TwoWay, cell);
                        AddIOCell(InnerCellsByTag, NetworkIOMode.Input, cell);
                        AddIOCell(InnerCellsByTag, NetworkIOMode.Output, cell);
                    }
                    if (c == _Input)
                        AddIOCell(InnerCellsByTag, NetworkIOMode.Input, cell);
                    if (c == _Output)
                        AddIOCell(InnerCellsByTag, NetworkIOMode.Output, cell);
                }
            }

            //Outer Connection Cells
            foreach (var edgeCell in thing.OccupiedRect().ExpandedBy(1).EdgeCells)
            {
                foreach (var inner in cellsInner)
                {
                    if (edgeCell.AdjacentToCardinal(inner))
                    {
                        cellsOuter.Add(edgeCell);
                        if (InnerModeFor(inner) == NetworkIOMode.TwoWay)
                        {
                            //TODO: Cleanup?
                            AddIOCell(OuterCellsByTag, NetworkIOMode.TwoWay, new IntVec3Rot(edgeCell, edgeCell.Rot4Relative(inner)));
                            AddIOCell(OuterCellsByTag, NetworkIOMode.Input, new IntVec3Rot(edgeCell, edgeCell.Rot4Relative(inner)));
                            AddIOCell(OuterCellsByTag, NetworkIOMode.Output, new IntVec3Rot(edgeCell, edgeCell.Rot4Relative(inner)));
                        }
                        if (InnerModeFor(inner) == NetworkIOMode.Input)
                            AddIOCell(OuterCellsByTag, NetworkIOMode.Input, new IntVec3Rot(edgeCell, edgeCell.Rot4Relative(inner)));
                        if (InnerModeFor(inner) == NetworkIOMode.Output)
                            AddIOCell(OuterCellsByTag, NetworkIOMode.Output, new IntVec3Rot(edgeCell, edgeCell.Rot4Relative(inner)));
                    }
                }
            }

            cachedInnerConnectionCells = cellsInner.ToArray();
            cachedConnectionCells = cellsOuter.ToArray();
        }
        
        private string GetCorrectPattern()
        {
            if (connectionPattern == null)
            {
                int widthx = thing.RotatedSize.x;
                int heighty = thing.RotatedSize.z;

                var charArr = new char[widthx * heighty];
                //Inner Connection Cells
                for (int y = 0; y < widthx; y++)
                {
                    for (int x = 0; x < heighty; x++)
                    {
                        charArr[x + (y*widthx)] = _TwoWay;
                    }
                }
                connectionPattern = charArr.ArrayToString();
            }

            return PatternByRot(thing.Rotation, thing.def.size);
        }

        private string PatternByRot(Rot4 rotation, IntVec2 size)
        {
            var newPattern = Regex.Replace(connectionPattern, regexIgnore, "").ToCharArray();
            if (!Regex.IsMatch(connectionPattern, regexMustHave))
            {
                TLog.Warning($"Pattern '{connectionPattern}' contains invalid characters. Allowed characters: ({_Input}, {_Output}, {_TwoWay}, {_Empty})");
            }

            int xWidth = size.x;
            int yHeight = size.z;
            if (rotation == Rot4.East)
            {
                return new string(newPattern.RotateLeft(xWidth, yHeight));
            }

            if (rotation == Rot4.South)
            {
                return new string(newPattern.FLipHorizontal(xWidth, yHeight));
            }

            if (rotation == Rot4.West)
            {
                return new string(newPattern.RotateRight(xWidth, yHeight));
            }

            return new string(newPattern);
        }
        
        //
        public void DrawIO()
        {
            if (OuterCellsByTag.TryGetValue(_TwoWay, out var twoway))
            {
                foreach (var cell in twoway)
                {
                    var drawPos = cell.IntVec.ToVector3Shifted();
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, cell.Rotation.AsQuat, BaseContent.BadMat, 0);
                    //GenDraw.DrawMeshNowOrLater(MeshPool.plane10, drawPos, cell.Rotation.AsQuat, BaseContent.BadMat, true);
                }
            }

            if (OuterCellsByTag.TryGetValue(_Output, out var output))
            {
                foreach (var cell in output)
                {
                    var drawPos = cell.IntVec.ToVector3Shifted();
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, cell.Rotation.AsQuat, TeleContent.IOArrow, 0);
                }
            }

            if (OuterCellsByTag.TryGetValue(_Input, out var input))
            {
                foreach (var cell in input)
                {
                    var drawPos = cell.IntVec.ToVector3Shifted();

                    Graphics.DrawMesh(MeshPool.plane10, drawPos, (cell.Rotation.AsAngle - 180).ToQuat(), TeleContent.IOArrow, 0);
                }
            }
        }

        public void PrintIO(SectionLayer layer)
        {
            if (OuterCellsByTag.TryGetValue(_TwoWay, out var twoway))
            {
                foreach (var cell in twoway)
                {
                    var drawPos = cell.IntVec.ToVector3Shifted();
                    TDrawing.PrintBasic(layer, drawPos, Vector2.one, BaseContent.BadMat, 0, false);
                }
            }

            if (OuterCellsByTag.TryGetValue(_Output, out var output))
            {
                foreach (var cell in output)
                {
                    var drawPos = cell.IntVec.ToVector3Shifted();
                    TDrawing.PrintBasic(layer, drawPos, Vector2.one, TeleContent.IOArrow, cell.Rotation.AsAngle, false);
                }
            }

            if (OuterCellsByTag.TryGetValue(_Input, out var input))
            {
                foreach (var cell in input)
                {
                    var drawPos = cell.IntVec.ToVector3Shifted();
                    TDrawing.PrintBasic(layer, drawPos, Vector2.one, TeleContent.IOArrow, (cell.Rotation.AsAngle - 180), false);
                }
            }
        }

        public bool ConnectsTo(NetworkCellIO otherGeneralIO, out IntVec3 intersectingCell, out NetworkIOMode IOMode)
        {
            intersectingCell = IntVec3.Invalid;
            IOMode = NetworkIOMode.None;
            for (var i = 0; i < OuterConnnectionCells.Length; i++)
            {
                var outerCell = OuterConnnectionCells[i];
                var outerMode = OuterModeFor(outerCell);
                if (otherGeneralIO.InnerConnnectionCells.Contains(outerCell) && Matches(otherGeneralIO.InnerModeFor(outerCell), outerMode))
                {
                    intersectingCell = outerCell;
                    IOMode = outerMode;
                    return true;
                }
            }

            return false;

            return OuterConnnectionCells.Where(otherGeneralIO.InnerConnnectionCells.Contains).Any(m => Matches(otherGeneralIO.InnerModeFor(m), OuterModeFor(m)));
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

        public bool Contains(IntVec3 cell)
        {
            return InnerConnnectionCells.Contains(cell);
        }
    }
}
