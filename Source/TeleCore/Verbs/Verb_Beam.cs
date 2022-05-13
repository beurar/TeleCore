using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore.Verbs
{
    public class Verb_Beam : Verb_Tele
    {
        public override DamageDef DamageDef => Props.beamProps.damageDef;

        protected override float ExplosionOnTargetSize => Props.beamProps.impactExplosion.explosionRadius;

        protected override BattleLogEntry_RangedFire EntryOnWarmupComplete()
        {
            return new BattleLogEntry_RangedFire(caster, currentTarget.HasThing ? currentTarget.Thing : null, EquipmentSource != null ? EquipmentSource.def : null, null, ShotsPerBurst > 1);
        }

        protected override bool IsAvailable()
        {
            return IsBeam;
        }

        protected override bool TryCastAttack()
        {
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
            Material mat = MaterialPool.MatFrom(beamProps.beamTexturePath, ShaderDatabase.MoteGlow);
            beam.solidTimeOverride = beamProps.solidTime;
            beam.fadeInTimeOverride = beamProps.fadeInTime;
            beam.fadeOutTimeOverride = beamProps.fadeOutTime;
            beam.AttachMaterial(mat, Color.white);
            beam.SetConnections(origin, targetPos);
            beam.Attach(caster);
            GenSpawn.Spawn(beam, caster.Position, caster.Map, WipeMode.Vanish);

            //Add extra glow to origin, if exists
            /*
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
            */
            Find.BattleLog.Add(new BattleLogEntry_RangedFire(this.caster, (!this.currentTarget.HasThing) ? null : this.currentTarget.Thing, (base.EquipmentSource == null) ? null : base.EquipmentSource.def, null, false));
            return true;
        }
    }
}
