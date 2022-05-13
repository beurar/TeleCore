using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class CompProperties_NetworkStructure : CompProperties
    {
        public List<NetworkComponentProperties> networks;
        public string connectionPattern;

        public CompProperties_NetworkStructure()
        {
            this.compClass = typeof(Comp_NetworkStructure);
        }

        public IntVec3[] InnerConnectionCells(Thing parent)
        {
            if (connectionPattern == null) return parent.OccupiedRect().ToArray();

            var pattern = PatternByRot(parent.Rotation, parent.def.size);
            var rect = parent.OccupiedRect();
            var rectList = rect.ToArray();
            var cellsInner = new List<IntVec3>();

            int width = parent.RotatedSize.x;
            int height = parent.RotatedSize.z;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int actualIndex = y * width + x;
                    int inv = ((height - 1) - y) * width + x;

                    var c = pattern[inv];
                    if (c == '+')
                        cellsInner.Add(rectList[actualIndex]);
                }
            }
            return cellsInner.ToArray();
        }

        private string PatternByRot(Rot4 rotation, IntVec2 size)
        {
            var patternArray = String.Concat(connectionPattern.Split('|')).ToCharArray();

            int xWidth = size.x;
            int yHeight = size.z;

            if (rotation == Rot4.East)
            {
                return new string(Rotate(patternArray, xWidth, yHeight, 0));
            }
            if (rotation == Rot4.South)
            {
                return new string(Rotate(patternArray, xWidth, yHeight, 1));
            }
            if (rotation == Rot4.West)
            {
                return new string(Rotate(patternArray, xWidth, yHeight, 2));
            }

            return new string(patternArray);
        }

        private char[] Rotate(char[] arr, int width, int height, int rotationInt = 0)
        {
            char[] newArray = new char[arr.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int indexToRotate = y * width + x;
                    int transposed = (x * height) + ((height - 1) - y);

                    newArray[transposed] = arr[indexToRotate];
                }
            }

            if (rotationInt > 0)
                return Rotate(newArray, height, width, --rotationInt);
            return newArray;
        }
    }
}
