using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TeleCore
{
    public class FilthSpawnerProperties
    {
        public List<DefValueLoadable<ThingDef>> filths;
        public float spreadRadius = 1.9f;

        public void SpawnFilth(IntVec3 center, Map map)
        {
            foreach (var cell in GenRadial.RadialCellsAround(center, spreadRadius, true))
            {
                foreach (var filth in filths)
                {
                    if (Rand.Chance(filth.Value.AsT1))
                    {
                        FilthMaker.TryMakeFilth(cell, map, filth.Def, 1);
                        break;
                    }
                }
            }
        }
    }
}
