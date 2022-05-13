using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class PositionOffSets
    {
        public List<Vector3> this[Rot4 rot]
        {
            get
            {
                if (rot == Rot4.North)
                {
                    return positionsNorth;
                }
                if (rot == Rot4.East)
                {
                    return positionsEast;
                }
                if (rot == Rot4.South)
                {
                    return positionsSouth;
                }
                if (rot == Rot4.West)
                {
                    return positionsWest;
                }
                return null;
            }
        }

        public List<Vector3> positionsNorth;
        public List<Vector3> positionsEast;
        public List<Vector3> positionsSouth;
        public List<Vector3> positionsWest;
    }
}
