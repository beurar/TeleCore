using System;
using System.Collections.Generic;
using Verse;
using Verse.Noise;

namespace TeleCore;

public class RandomAdjacentCellComparer : IComparer<IntVec3>
{
    private const float NoisyEdgeFactor = 0.25f;
    private const float PerlinNoiseFactor = 2f;
    private readonly IntVec3 root;
    private readonly Map map;
    private readonly ModuleBase noiseGenerator;
    private readonly Predicate<IntVec3> validator;

    public RandomAdjacentCellComparer(IntVec3 root, Map map, Predicate<IntVec3> adjacentValidator, float frequency = 0.015f)
    {
        this.root = root;
        this.map = map;
        this.validator = adjacentValidator;
        noiseGenerator = new Perlin(frequency, 2.0, 0.5, 6, map.uniqueID, QualityMode.Medium);
        noiseGenerator = new ScaleBias(0.45, 0.65, noiseGenerator);
    }

    private float CellScore(IntVec3 c)
    {
        var num = 1f;
        num *= 1f / c.DistanceTo(root);
        num *= 1f + (float) noiseGenerator.GetValue(c.x, c.y, c.z) * PerlinNoiseFactor;
        if (MapGenerator.mapBeingGenerated == map)
            num *= c.DistanceTo(MapGenerator.PlayerStartSpot) / map.Size.LengthHorizontal;
        return num * (1f + AdjacentCellCount(c) / 8f * NoisyEdgeFactor);
    }
    
    private int AdjacentCellCount(IntVec3 c)
    {
        int num = 0;
        for (int i = 0; i < GenAdj.AdjacentCells.Length; i++)
        {
            IntVec3 intVec = GenAdj.AdjacentCells[i];
            IntVec3 c2 = c + intVec;
            if (c2.InBounds(map) && validator(c2))
            {
                num++;
            }
        }
        return num;
    }
    
    public int Compare(IntVec3 a, IntVec3 b)
    {
        float num = CellScore(a);
        float num2 = CellScore(b);
        
        if (num < num2) return 1;
        if (num > num2) return -1;
        return 0;
    }
}