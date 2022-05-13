using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public static class CellGen
    {

        /// <summary>
        /// Calculates a circular sector of <see cref="IntVec3"/> cells. Recommended to cache the result.
        /// </summary>
        /// <param name="center">Center of circle.</param>
        /// <param name="map">Map reference.</param>
        /// <param name="radius">Radius of circle.</param>
        /// <param name="angle">Angle size of the sector.</param>
        /// <param name="rotation">Rotation of the sector, 0 would make the sector "go" upwards.</param>
        /// <param name="useCenter">Uses the center cells as part of the sector.</param>
        /// <param name="validator">Additional validator to exclude or include cells.</param>
        public static IEnumerable<IntVec3> SectorCells(IntVec3 center, Map map, float radius, float angle, float rotation, bool useCenter = false, Predicate<IntVec3> validator = null)
        {
            int cellCount = GenRadial.NumCellsInRadius(radius);
            int startCell = useCenter ? 0 : 1;
            var angleMin = (rotation - angle * 0.5f).AngleWrapped();
            var angleMax = (angleMin + angle).AngleWrapped();
            for (int i = startCell; i < cellCount; i++)
            {
                IntVec3 cell = GenRadial.RadialPattern[i] + center;
                float curAngle = (cell.ToVector3Shifted() - center.ToVector3Shifted()).AngleFlat();
                var invert = angleMin > angleMax;
                var flag = invert ? (curAngle >= angleMin || curAngle <= angleMax) : (curAngle >= angleMin && curAngle <= angleMax);
                if (map != null && !cell.InBounds(map) || !flag || (validator != null && !validator(cell)))
                    continue;
                yield return cell;
            }
        }

        public static CellRect ToCellRect(this List<IntVec3> cells)
        {
            int minZ = cells.Min(c => c.z);
            int maxZ = cells.Max(c => c.z);
            int minX = cells.Min(c => c.x);
            int maxX = cells.Max(c => c.x);
            int width = maxX - (minX - 1);
            int height = maxZ - (minZ - 1);
            return new CellRect(minX, minZ, width, height);
        }

        public static IEnumerable<IntVec3> CellsAdjacent8Way(this IntVec3 loc, bool andInside = false)
        {
            if (andInside)
            { yield return loc; }

            IntVec3 center = loc;
            int minX = center.x - (1 - 1) / 2 - 1;
            int minZ = center.z - (1 - 1) / 2 - 1;
            int maxX = minX + 1 + 1;
            int maxZ = minZ + 1 + 1;
            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minZ; j <= maxZ; j++)
                {
                    yield return new IntVec3(i, 0, j);
                }
            }
            yield break;
        }
    }
}
