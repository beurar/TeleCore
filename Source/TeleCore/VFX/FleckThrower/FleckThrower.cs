using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class FleckThrower
    {
        private Thing parent;
        private FleckThrowerInfo info;

        //
        private int ticksLeft = 0;
        private int ticksUntilBurst = 0;
        private int burstLeft = 0;

        public FleckThrower(FleckThrowerInfo info, Thing parent)
        {
            this.parent = parent;
            this.info = info;
        }

        public void ThrowerTick(Vector3 pos, Map map)
        {
            switch (info.type)
            {
                case FleckThrowerType.OnTick:
                    if (info.burstInterval.Average > 0)
                        if (ticksUntilBurst > 0)
                            ticksUntilBurst--;
                        else if (burstLeft > 0)
                        {
                            burstLeft--;
                            TryBurstFlecks(pos, map);
                        }
                        else
                            ResetBurst();
                    else
                    {
                        ticksLeft--;
                        if (ticksLeft <= 0)
                        {
                            TryBurstFlecks(pos, map);
                            ticksLeft = info.throwInterval.RandomInRange;
                        }
                    }
                    return;
                case FleckThrowerType.OnChance:
                    if (Rand.Chance(info.chancePerTick))
                    {
                        TryBurstFlecks(pos, map);
                    }
                    return;
            }
        }

        private void ResetBurst()
        {
            ticksUntilBurst = info.burstInterval.RandomInRange;
            burstLeft = info.burstRange.RandomInRange;
        }

        private void TryBurstFlecks(Vector3 exactPos, Map map)
        {
            IntVec3 spawnPos = exactPos.ToIntVec3();
            if (!spawnPos.InBounds(map)) return;
            int burstCount = info.burstCount.RandomInRange;
            for (int i = 0; i < burstCount; i++)
            {
                MakeSingleFleck(exactPos, map);
            }
        }

        private void MakeSingleFleck(Vector3 exactPos, Map map)
        {
            var spawnPos = exactPos + info.positionOffset + Gen.RandomHorizontalVector(info.positionRadius);

            FleckCreationData dataStatic = FleckMaker.GetDataStatic(spawnPos, map, info.fleckDef, info.scale.RandomInRange);

            //dataStatic.spawnPosition = spawnPos;
            dataStatic.rotation = info.rotation.RandomInRange;
            dataStatic.rotationRate = info.rotationRate.RandomInRange;
            dataStatic.solidTimeOverride = info.solidTime.Average > 0 ? info.solidTime.RandomInRange : -1f;

            dataStatic.airTimeLeft = info.airTime.RandomInRange;
            dataStatic.velocitySpeed = info.speed.RandomInRange;
            dataStatic.velocityAngle = info.angle.RandomInRange;

            float speed = info.speed.RandomInRange;
            float angle = info.angle.RandomInRange;

            if (info.affectedByWind)
            {
               var windSpeed = parent.GetRoom().PsychologicallyOutdoors ? map.windManager.WindSpeed : 0f;
               float windPct = Mathf.InverseLerp(0f, 2f, windSpeed);
               speed *= Mathf.Lerp(0.1f, 1, windPct);
               angle = (int)Mathf.Lerp(info.angle.min, info.angle.max, windPct);
            }
            dataStatic.velocitySpeed = speed;
            dataStatic.velocityAngle = angle;

            map.flecks.CreateFleck(dataStatic);
        }
    }
}
