using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.Static;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TeleCore
{
	public class Verb_Beam : Verb_Tele
    {
	    //
	    private Vector3 initialTargetPosition;
	    private List<Vector3> path = new List<Vector3>();
	    private int ticksToNextPathStep;

	    private MoteDualAttached beamMote;
	    private Effecter endEffecter;
	    private Sustainer sustainer;

	    protected BeamProperties BeamProps => Props.beamProps;
	    
	    //
	    public override DamageDef DamageDef => BeamProps.damageDef;
	    protected override float ExplosionOnTargetSize => BeamProps.impactExplosion?.explosionRadius ?? 0;

        
        protected override BattleLogEntry_RangedFire EntryOnWarmupComplete()
        {
            return new BattleLogEntry_RangedFire(caster, currentTarget.HasThing ? currentTarget.Thing : null, EquipmentSource != null ? EquipmentSource.def : null, null, ShotsPerBurst > 1);
        }
        
        //Beam Moving Mechanic
        private float ShotProgress => ticksToNextPathStep / (float) verbProps.ticksBetweenBurstShots;

        private Vector3 InterpolatedPosition => Vector3.Lerp(path[burstShotsLeft], path[Mathf.Min(burstShotsLeft + 1, path.Count - 1)], ShotProgress) 
                                                + (CurrentTarget.CenterVector3 - initialTargetPosition);

        public override float? AimAngleOverride
        {
	        get
	        {
		        if (state != VerbState.Bursting) return null;
		        return (InterpolatedPosition - caster.DrawPos).AngleFlat();
	        }
        }
        
        public override void ExposeData()
        {
	        base.ExposeData();
	        Scribe_Collections.Look(ref path, "path", LookMode.Value, Array.Empty<object>());
	        Scribe_Values.Look(ref ticksToNextPathStep, "ticksToNextPathStep");
	        Scribe_Values.Look(ref initialTargetPosition, "initialTargetPosition");
	        if (Scribe.mode == LoadSaveMode.PostLoadInit && path == null)
	        {
		        path = new List<Vector3>();
	        }
        }

        protected override bool IsAvailable()
        {
            return IsBeam;
        }
        
        public override void WarmupComplete()
        {
	        burstShotsLeft = ShotsPerBurst;
	        state = VerbState.Bursting;
	        initialTargetPosition = currentTarget.CenterVector3;
	        path.Clear();
	        var rangeDiff = (currentTarget.CenterVector3 - ShotOrigin.Yto0());
	        var magnitude = rangeDiff.magnitude;
	        var normalized = rangeDiff.normalized;
	        var a = normalized.RotatedBy(-90f);
	        var num = verbProps.beamFullWidthRange > 0f ? Mathf.Min(magnitude / verbProps.beamFullWidthRange, 1f) : 1f;
	        var d = (verbProps.beamWidth + 1f) * num / ShotsPerBurst;
	        var vector2 = currentTarget.CenterVector3.Yto0() - a * verbProps.beamWidth * 0.5f * num;
	        path.Add(vector2);
	        
	        //
	        for (var i = 0; i < ShotsPerBurst; i++)
	        {
		        var a2 = normalized * (Rand.Value * verbProps.beamMaxDeviation) - normalized * 0.5f;
		        var b = Mathf.Sin((i / (float) ShotsPerBurst + 0.5f) * Mathf.PI * 57.29578f) * verbProps.beamCurvature * -normalized - normalized * verbProps.beamMaxDeviation * 0.5f;
		        path.Add(vector2 + (a2 + b) * num);
		        vector2 += a * d;
	        }

	        //Set BeamMote
	        if (verbProps.beamMoteDef != null)
	        {
		        beamMote = MoteMaker.MakeInteractionOverlay(verbProps.beamMoteDef, caster, new TargetInfo(path[0].ToIntVec3(), caster.Map));
	        }

	        TryCastNextBurstShot();
	        
	        ticksToNextPathStep = verbProps.ticksBetweenBurstShots;
	        endEffecter?.Cleanup();
	        if (verbProps.soundCastBeam != null)
	        {
		        sustainer = verbProps.soundCastBeam.TrySpawnSustainer(SoundInfo.InMap(caster, MaintenanceType.PerTick));
	        }
        }

        //Does the wandering
        public override void BurstingTick()
        {
	        //Enter next step
			ticksToNextPathStep--;
			var curPos = InterpolatedPosition;
			var curPosIntVec = curPos.ToIntVec3();
			var rangeDiff = InterpolatedPosition - ShotOrigin;
			var magnitude = rangeDiff.MagnitudeHorizontal();
			var normalized = rangeDiff.Yto0().normalized;
			var b = GenSight.LastPointOnLineOfSight(caster.Position, curPosIntVec, c => c.CanBeSeenOverFast(caster.Map), true);

			if (b.IsValid)
			{
				//Have valid target pos
				magnitude -= (curPosIntVec - b).LengthHorizontal;
				curPos = caster.Position.ToVector3Shifted() + normalized * magnitude;
				curPosIntVec = curPos.ToIntVec3();
			}
			Vector3 offsetA = normalized * verbProps.beamStartOffset;
			Vector3 vector3 = curPos - curPosIntVec.ToVector3Shifted();
			if (beamMote != null)
			{
				beamMote.UpdateTargets(new TargetInfo(caster.Position, caster.Map), new TargetInfo(curPosIntVec, caster.Map), offsetA + CurrentShotOffset, vector3);
				beamMote.Maintain();
			}

			if (verbProps.beamGroundFleckDef != null && Rand.Chance(verbProps.beamFleckChancePerTick))
			{
				FleckMaker.Static(curPos, caster.Map, verbProps.beamGroundFleckDef);
			}

			if (endEffecter == null && verbProps.beamEndEffecterDef != null)
			{
				endEffecter = verbProps.beamEndEffecterDef.Spawn(curPosIntVec, caster.Map, vector3);
			}

			//Do endeffecter
			if (endEffecter != null)
			{
				endEffecter.offset = vector3;
				endEffecter.EffectTick(new TargetInfo(curPosIntVec, caster.Map), TargetInfo.Invalid);
				endEffecter.ticksLeft--;
			}

			//Spawn impact fleck
			if (verbProps.beamLineFleckDef != null)
			{
				var num2 = 1f * magnitude;
				var num3 = 0;
				while (num3 < num2)
				{
					if (Rand.Chance(verbProps.beamLineFleckChanceCurve.Evaluate(num3 / num2)))
					{
						var b2 = num3 * normalized - normalized * Rand.Value + normalized / 2f;
						FleckMaker.Static(caster.Position.ToVector3Shifted() + b2, caster.Map,
							verbProps.beamLineFleckDef);
					}
					num3++;
				}
			}

			//
			sustainer?.Maintain();
        }
        
        
        protected override bool TryCastAttack()
        {
	        if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map) return false;
	        if (verbProps.stopBurstWithoutLos && !TryFindShootLineFromTo(caster.Position, currentTarget, out _)) return false;
	        
	        
	        lastShotTick = Find.TickManager.TicksGame;
	        ticksToNextPathStep = verbProps.ticksBetweenBurstShots;
	        var desiredRange = InterpolatedPosition.Yto0().ToIntVec3();
	        var rangePos = GenSight.LastPointOnLineOfSight(caster.Position, desiredRange, c => c.CanBeSeenOverFast(caster.Map), true);
	       
	        HitCell(rangePos.IsValid ? rangePos : desiredRange);
	        return true;
	        
	        /*
            //If no target, abort
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map) return false;

            //If cant get a shootline to target, abort
            ShootLine shootLine = new ShootLine(caster.Position, currentTarget.Cell);
            if (verbProps.stopBurstWithoutLos && !TryFindShootLineFromTo(caster.Position, currentTarget, out shootLine)) return false;

            //Get target with offset...
            LocalTargetInfo adjustedTarget = AdjustedTarget(currentTarget, ref shootLine, out ProjectileHitFlags flags);
            var beamProps = Props.beamProps;
            DamageDef damage = beamProps.damageDef ?? DamageDefOf.Burn;
            if (adjustedTarget.HasThing)
            {
                adjustedTarget.Thing.TakeDamage(new DamageInfo(damage, beamProps.damageBase, beamProps.armorPenetration, -1, caster, null, GunDef, DamageInfo.SourceCategory.ThingOrUnknown, currentTarget.Thing));
                Pawn hitPawn = adjustedTarget.Thing as Pawn;
                if (hitPawn?.stances != null && hitPawn.BodySize <= beamProps.stoppingPower + 0.001f)
                {
                    hitPawn.stances.StaggerFor(beamProps.staggerTime.SecondsToTicks());
                }
            }

            //Do Visual Effects
            Vector3 origin = ShotOrigin;
            Vector3 targetPos = adjustedTarget.Cell.ToVector3Shifted();

            //Do impact effects
            Props.beamProps.impactEffecter?.Spawn(adjustedTarget.Cell, caster.Map);
            Props.beamProps.impactExplosion?.DoExplosion(adjustedTarget.Cell, caster.Map, Caster);
            Props.beamProps.impactFilth?.SpawnFilth(adjustedTarget.Cell, caster.Map);

            //Spawn beam effect
            Mote_Beam beam = (Mote_Beam)ThingMaker.MakeThing(TeleDefOf.Mote_Beam);
            beam.solidTimeOverride = beamProps.solidTime;
            beam.fadeInTimeOverride = beamProps.fadeInTime;
            beam.fadeOutTimeOverride = beamProps.fadeOutTime;
            beam.AttachMaterial(beamProps.BeamMat, Color.white);
            beam.SetConnections(origin, targetPos);
            beam.Attach(caster);
            GenSpawn.Spawn(beam, caster.Position, caster.Map, WipeMode.Vanish);

            //Add extra glow to origin, if exists
            
            if (beamProps.glow != null)
            {
                //TODO: Replace motes with more fine-tuned settings (eg. fade-in and -out time)
                MoteThrown glow = (MoteThrown)ThingMaker.MakeThing(beamProps.glow.glowMote //DefDatabase<ThingDef>.GetNamed("ObeliskGlow"));
                glow.exactPosition = origin;
                glow.Scale = beamProps.glow.scale;
                glow.exactRotation = beamProps.glow.rotation;
                glow.rotationRate = beamProps.glow.rotationRate;
                glow.airTimeLeft = 99999;
                glow.SetVelocity(0, 0);
                GenSpawn.Spawn(glow, caster.Position + IntVec3.East, caster.Map);
            }
            
            Find.BattleLog.Add(new BattleLogEntry_RangedFire(this.caster, (!this.currentTarget.HasThing) ? null : this.currentTarget.Thing, (base.EquipmentSource == null) ? null : base.EquipmentSource.def, null, false));
            return true;
            */
        }
        
        //Hit Logic
        private bool CanHit(Thing thing)
        {
	        return thing.Spawned && !CoverUtility.ThingCovered(thing, caster.Map);
        }

        private void HitCell(IntVec3 cell)
        {
	        ApplyDamage(VerbUtility.ThingsToHit(cell, caster.Map, CanHit).RandomElementWithFallback());
        }
        
        private void ApplyDamage(Thing thing)
        {
	        var intVec = InterpolatedPosition.Yto0().ToIntVec3();
	        var intVec2 = GenSight.LastPointOnLineOfSight(caster.Position, intVec, c => c.CanBeSeenOverFast(caster.Map), true);
	        if (intVec2.IsValid)
	        {
		        intVec = intVec2;
	        }
	        if (thing != null && verbProps.beamDamageDef != null)
	        {
		        var angleFlat = (currentTarget.Cell - caster.Position).AngleFlat;
		        var log = new BattleLogEntry_RangedImpact(EquipmentSource, thing, currentTarget.Thing, EquipmentSource.def, null, null);
		        var dinfo = new DamageInfo(verbProps.beamDamageDef, BeamProps.damageBase, verbProps.beamDamageDef.defaultArmorPenetration, angleFlat, EquipmentSource, null, EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, currentTarget.Thing);
		        thing.TakeDamage(dinfo).AssociateWithLog(log);
		        if (thing.CanEverAttachFire())
		        {
			        if (Rand.Chance(verbProps.beamChanceToAttachFire))
				        thing.TryAttachFire(verbProps.beamFireSizeRange.RandomInRange);
		        }
		        else if (Rand.Chance(verbProps.beamChanceToStartFire))
		        {
			        FireUtility.TryStartFireIn(intVec, caster.Map, verbProps.beamFireSizeRange.RandomInRange);
		        }
	        }
        }
    }
}
