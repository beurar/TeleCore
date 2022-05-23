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
    public abstract class Verb_Tele : Verb
    {
        //Turret Barrel Offset
        private int lastOffsetIndex = 0;
        private int currentOffsetIndex = 0;
        private int maxOffsetCount = 1;

        //
        public TurretGun turretGun;

        //
        public VerbProperties_Extended Props => (VerbProperties_Extended)verbProps;
        public Comp_NetworkStructure NetworkComp => caster.TryGetComp<Comp_NetworkStructure>();
        public CompPowerTrader PowerComp => caster.TryGetComp<CompPowerTrader>();

        public ThingDef GunDef
        {
            get
            {
                if (CasterIsPawn)
                    return EquipmentSource.def;
                if (turretGun != null)
                    return turretGun.Gun.def;
                return caster.def.building.turretGunDef;
            }
        }

        public bool IsBeam => Props.beamProps != null;
        public bool IsMortar => !IsBeam && Props.defaultProjectile.projectile.flyOverhead;

        public virtual DamageDef DamageDef => null;
        public virtual ThingDef Projectile => null;
        protected virtual float ExplosionOnTargetSize => 0;

        public override int ShotsPerBurst => verbProps.burstShotCount;

        //Origin Offsetting
        private int OffsetIndex => turretGun?.ShotIndex ?? currentOffsetIndex;
        protected Vector3 DrawPos => turretGun?.DrawPos ?? caster.DrawPos;

        protected Vector3 CurrentShotOffset
        {
            get
            {
                if (Props.originOffset != Vector3.zero)
                {
                    return Props.originOffset;
                }
                if (!Props.originOffsets.NullOrEmpty())
                    return Props.originOffsets[OffsetIndex];
                return Vector3.zero;
            }
        }

        protected Vector3 ShotOrigin
        {
            get
            {
                Vector3 offset = Vector3.zero;
                if (turretGun?.Top != null && turretGun.Top.Props.barrelMuzzleOffset != Vector3.zero)
                {
                    offset = turretGun.Top.Props.barrelMuzzleOffset;
                }

                offset += CurrentShotOffset;
                return DrawPos + offset.RotatedBy(GunRotation);
            }
        }

        protected float GunRotation
        {
            get
            {
                if (CasterIsPawn)
                {
                    Vector3 a;
                    float num = 0f;
                    if (CasterPawn.stances.curStance is Stance_Busy {neverAimWeapon: false, focusTarg: {IsValid: true}} stance)
                    {
                        if (stance.focusTarg.HasThing)
                        {
                            a = stance.focusTarg.Thing.DrawPos;
                        }
                        else
                        {
                            a = stance.focusTarg.Cell.ToVector3Shifted();
                        }

                        if ((a - CasterPawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                        {
                            num = (a - CasterPawn.DrawPos).AngleFlat();
                        }

                        return num;
                    }
                }
                return turretGun?.TurretRotation ?? 0f;
            }
        }

        //Barrel Rotation
        private void RotateNextShotIndex()
        {
            lastOffsetIndex = currentOffsetIndex;
            currentOffsetIndex++;
            if (currentOffsetIndex > (maxOffsetCount - 1))
                currentOffsetIndex = 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastOffsetIndex, "lastOffsetIndex");
            Scribe_Values.Look(ref currentOffsetIndex, "currentOffsetIndex");
            Scribe_Values.Look(ref maxOffsetCount, "maxOffsetCount");
        }

        public override void Reset()
        {
            base.Reset();
            maxOffsetCount = Props.originOffsets?.Count ?? 0;
        }

        private void Notify_SingleShot()
        {
            if (turretGun != null)
                turretGun.Notify_FiredSingleProjectile();
            else
                RotateNextShotIndex();
        }

        //
        public override bool IsUsableOn(Thing target)
        {
            return true;
        }

        protected virtual bool TryCastAttack()
        {
            return false;
        }

        protected virtual bool IsAvailable()
        {
            return true;
        }

        protected virtual BattleLogEntry_RangedFire EntryOnWarmupComplete()
        {
            return null;
        }

        protected virtual void CustomTick()
        {
            throw new NotImplementedException();
        }

        //Base Tele Behaviour
        public override void WarmupComplete()
        {
            burstShotsLeft = ShotsPerBurst;
            state = VerbState.Bursting;
            TryCastNextBurstShot();
            var entry = EntryOnWarmupComplete();
            if (entry != null)
            {
                Find.BattleLog.Add(entry);
            }
        }

        public sealed override bool Available()
        {
            if (!base.Available()) return false;

            //TODO: Add power consumption
            /*
            if (Props.powerConsumptionPerShot > 0)
            {
                PowerComp.PowerNet.batteryComps.Any(t => t.StoredEnergy > Props.powerConsumptionPerShot);
            }
            */

            if (Props.networkCostPerShot != null)
            {
                return Props.networkCostPerShot.CanPayWith(NetworkComp);
            }

            if (CasterIsPawn)
            {
                Pawn casterPawn = CasterPawn;
                if (casterPawn.Faction != Faction.OfPlayer && casterPawn.mindState.MeleeThreatStillThreat &&
                    casterPawn.mindState.meleeThreat.Position.AdjacentTo8WayOrInside(casterPawn.Position))
                {
                    return false;
                }
            }

            return IsAvailable();
        }

        public sealed override bool TryCastShot()
        {
            var flag = TryCastAttack();
            if (!flag) return false;

            //Do Origin Effect if exists
            if (Props.originEffecter != null)
            {
                Props.originEffecter.Spawn(caster.Position, caster.Map, CurrentShotOffset);
            }

            //Did Shot
            Notify_SingleShot();

            if (verbProps.consumeFuelPerShot > 0f)
            {
                turretGun?.RefuelComp?.ConsumeFuel(verbProps.consumeFuelPerShot);
            }

            //TODO: Add power consumption
            if (Props.powerConsumptionPerShot > 0)
            {

            }

            if (Props.networkCostPerShot != null)
            {
                if (Props.networkCostPerShot.CanPayWith(NetworkComp))
                    Props.networkCostPerShot.DoPayWith(NetworkComp);
                else
                    return false;
            }

            if (base.CasterIsPawn)
            {
                base.CasterPawn.records.Increment(RecordDefOf.ShotsFired);
            }
            return true;
        }

        //
        /// <summary>
        /// Applies the vanilla target "miss" chance on an intended target
        /// </summary>
        protected LocalTargetInfo AdjustedTarget(LocalTargetInfo intended, ref ShootLine shootLine, out ProjectileHitFlags flags)
        {
            flags = ProjectileHitFlags.NonTargetWorld;
            if (verbProps.ForcedMissRadius > 0.5f)
            {
                float num = VerbUtility.CalculateAdjustedForcedMiss(verbProps.ForcedMissRadius, intended.Cell - caster.Position);
                if (num > 0.5f)
                {
                    if (Rand.Chance(0.5f))
                        flags = ProjectileHitFlags.All;
                    if (!canHitNonTargetPawnsNow)
                        flags &= ~ProjectileHitFlags.NonTargetPawns;

                    int max = GenRadial.NumCellsInRadius(num);
                    int num2 = Rand.Range(0, max);
                    if (num2 > 0)
                    {
                        return GetTargetFromPos((intended.Cell + GenRadial.RadialPattern[num2]), caster.Map);
                    }
                }
            }
            ShotReport shotReport = ShotReport.HitReportFor(caster, this, intended);
            Thing cover = shotReport.GetRandomCoverToMissInto();
            if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                    flags |= ProjectileHitFlags.NonTargetPawns;
                shootLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
                return GetTargetFromPos(shootLine.Dest, caster.Map);
            }
            if (intended.Thing != null && intended.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.PassCoverChance))
            {
                if (canHitNonTargetPawnsNow)
                    flags |= ProjectileHitFlags.NonTargetPawns;
                return cover;
            }
            return intended;
        }

        protected LocalTargetInfo GetTargetFromPos(IntVec3 pos, Map map)
        {
            var things = pos.GetThingList(map);
            if (things.NullOrEmpty()) return pos;
            return things.MaxBy(t => t.def.altitudeLayer);
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            return ExplosionOnTargetSize;
        }
    }
}
