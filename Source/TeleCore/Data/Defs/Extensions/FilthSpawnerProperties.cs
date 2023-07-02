using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TeleCore;

public class FilthSpawnerProperties
{
    public List<DefFloatRef<ThingDef>> filths;
    public float spreadRadius = 1.9f;

    public void SpawnFilth(IntVec3 center, Map map)
    {
        foreach (var cell in GenRadial.RadialCellsAround(center, spreadRadius, true))
        foreach (var filth in filths)
            if (Rand.Chance(filth.Value))
            {
                FilthMaker.TryMakeFilth(cell, map, filth.Def);
                break;
            }
    }
}