using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public static class CellUtils
    {
        public static int Index(this IntVec3 vec, Map map)
        {
            return map.cellIndices.CellToIndex(vec);
        }
    }
}
