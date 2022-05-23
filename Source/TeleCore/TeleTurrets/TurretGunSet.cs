using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Multiplayer.API;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class TurretGunSet : IExposable
    {
        //
        private readonly List<LocalTargetInfo> knownTargetsList = new();

        //
        private readonly ITurretHolder parent;
        private readonly TurretGun mainGun;
        private readonly List<TurretGun> turrets;

        //
        private bool holdFire = false;

        public List<LocalTargetInfo> KnownTargets => knownTargetsList;

        //
        public List<TurretGun> Turrets => turrets;
        public TurretGun MainGun => mainGun;
        public Verb AttackVerb => MainGun.AttackVerb;

        //
        public bool HoldingFire => holdFire;

        public TurretGunSet(TurretDefExtension holderProps, ITurretHolder parent)
        {
            this.parent = parent;
            turrets = new List<TurretGun>(holderProps.turrets.Count);
            var set = this;
            for (var i = 0; i < holderProps.turrets.Count; i++)
            {
                var props = holderProps.turrets[i];
                var turret = (TurretGun) Activator.CreateInstance(props.turretGunClass);
                turret.Setup(props, i, set, parent);
                turrets.Add(turret);
                mainGun ??= turret;
            }
        }

        public TurretGunSet(List<TurretProperties> turretProps, ITurretHolder parent)
        {
            this.parent = parent;
            turrets = new List<TurretGun>(turretProps.Count);
            var set = this;
            for (var i = 0; i < turretProps.Count; i++)
            {
                var props = turretProps[i];
                var turret = (TurretGun)Activator.CreateInstance(props.turretGunClass, new { props, i, set, parent });
                turrets.Add(turret);
                mainGun ??= turret;
            }
        }

        public void ExposeData()
        {

        }

        public void TickTurrets()
        {
            foreach (TurretGun turret in turrets)
            {
                turret.TurretTick();
            }
        }

        //
        public void TryOrderAttack(LocalTargetInfo targ)
        {
            foreach (var turretGun in turrets)
            {
                turretGun.TryOrderAttack(targ);
            }
        }

        [SyncMethod]
        public void CommandHoldFire()
        {
            holdFire = !holdFire;
            if (holdFire)
            {
                ResetOrderedAttack();
            }
        }

        public void ResetOrderedAttack()
        {
            turrets.ForEach(t => t.ResetForcedTarget());
        }

        //
        public bool HasTarget(LocalTargetInfo target)
        {
            return KnownTargets.Contains(target);
        }

        public void Notify_NewTarget(LocalTargetInfo currentTarget)
        {
            if (HasTarget(currentTarget)) return;
            KnownTargets.Add(currentTarget);
        }

        public void Notify_LostTarget(LocalTargetInfo target)
        {
            KnownTargets.Remove(target);
        }

        //Drawing
        public void Draw()
        {
            foreach (TurretGun gun in turrets)
            {
                gun.Draw();
            }
        }

        public IEnumerable<Gizmo> TurretGizmos()
        {
            yield return new Command_Toggle
            {
                defaultLabel = "CommandHoldFire".Translate(),
                defaultDesc = "CommandHoldFireDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire", true),
                toggleAction = CommandHoldFire,
                isActive = (() => holdFire)
            };
        }

        public string InspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Active turrets: {turrets.Count}");
            sb.AppendLine($"PlayerControlled: {this.parent.PlayerControlled}");
            sb.AppendLine($"CanSetForcedTarget: {MainGun.CanSetForcedTarget}");
            if (!Enumerable.Any(turrets)) return sb.ToString().TrimEndNewlines();

            sb.AppendLine("-- Main Turret --");
            if (AttackVerb.verbProps.minRange > 0f)
            {
                sb.AppendLine("MinimumRange".Translate() + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
            }
            var parent = this.parent.HolderThing;
            if (parent.Spawned && MainGun.NeedsRoof && parent.Position.Roofed(parent.Map))
            {
                sb.AppendLine("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
            }
            else if (parent.Spawned && MainGun.BurstCoolDownTicksLeft > 0 && MainGun.BurstCooldownTime() > 5f)
            {
                sb.AppendLine("CanFireIn".Translate() + ": " + MainGun.BurstCoolDownTicksLeft.ToStringSecondsFromTicks());
            }
            CompChangeableProjectile compChangeableProjectile = MainGun.Gun.TryGetComp<CompChangeableProjectile>();
            if (compChangeableProjectile != null)
            {
                if (compChangeableProjectile.Loaded)
                    sb.AppendLine("ShellLoaded".Translate(compChangeableProjectile.LoadedShell.LabelCap, compChangeableProjectile.LoadedShell));
                else
                    sb.AppendLine("ShellNotLoaded".Translate());
            }
            return sb.ToString();
        }
    }
}
