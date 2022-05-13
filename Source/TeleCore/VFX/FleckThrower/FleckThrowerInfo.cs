using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public enum FleckThrowerType
    {
        OnTick,
        OnChance
    }

    public class FleckThrowerInfo
    {
        public FleckDef fleckDef;
        public SoundDef soundDef;

        public FleckThrowerType type = FleckThrowerType.OnTick;
        public FloatRange speed = FloatRange.Zero;
        public FloatRange scale = FloatRange.One;
        public FloatRange rotation = new FloatRange(0f, 360f);
        public FloatRange rotationRate = new FloatRange(0f, 0f);
        public FloatRange angle = new FloatRange(0f, 360f);
        public FloatRange airTime = new FloatRange(999999f, 999999f);
        public IntRange burstCount = new IntRange(1, 1);
        public IntRange burstRange = new IntRange(100, 100);

        //public Color color = Color.white;
        public Vector3 positionOffset = Vector3.zero;
        public FloatRange solidTime = FloatRange.Zero;
        public float positionRadius = 0;
        public bool affectedByWind = false;

        //Intervals
        public IntRange burstInterval = new IntRange(0, 0);
        public IntRange throwInterval = new IntRange(40, 100);
        public IntRange soundInterval = new IntRange(40, 100);

        public float chancePerTick = 0.1f;
    }
}
