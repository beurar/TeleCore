using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Multiplayer.API;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TeleCore
{
    /// <summary>
    /// 
    /// </summary>
    public class Building_TeleTurret : Building_Turret, ITurretHolder, IFXLayerProvider
    {
        private CompPowerTrader powerComp;
        private CompCanBeDormant dormantComp;
        private CompInitiatable initiatableComp;
        private CompMannable mannableComp;
        private CompRefuelable refuelComp;
        private Comp_Network networkComp;

        //
        private TurretDefExtension defExtension;
        private TurretGunSet turretSet;

        private bool canForceTargetDefault;

        public TurretDefExtension Extension => defExtension;

        //
        public virtual LocalTargetInfo TargetOverride => base.forcedTarget;

        //
        public override LocalTargetInfo CurrentTarget => turretSet.KnownTargets.First();
        public override Verb AttackVerb => turretSet.AttackVerb;
        public Verb_Tele TeleVerb => turretSet.AttackVerb as Verb_Tele;
        public TurretGun MainGun => turretSet.MainGun;

        //TurretHolder
        public bool IsActive => Spawned && (PowerComp == null || PowerComp.PowerOn) && (MannableComp == null || MannableComp.MannedNow);
        public bool PlayerControlled => Faction == Faction.OfPlayer || MannedByColonist;
        public bool MannedByColonist => ManningPawn?.Faction == Faction.OfPlayer;
        public bool MannedByNonColonist => ManningPawn?.Faction != Faction.OfPlayer;
        public bool HoldingFire => turretSet.HoldingFire;
        
        //
        public Pawn ManningPawn => MannableComp?.ManningPawn;

        public virtual Thing Caster => this;
        public virtual Thing HolderThing => this;
        public new virtual CompPowerTrader PowerComp => powerComp;
        public virtual CompCanBeDormant DormantComp => dormantComp;
        public virtual CompInitiatable InitiatableComp => initiatableComp;
        public virtual CompMannable MannableComp => mannableComp;
        public virtual CompRefuelable RefuelComp => refuelComp;
        public virtual Comp_Network NetworkComp => networkComp;
        public virtual StunHandler Stunner => stunner;
        
        #region FX Implementation
        
        //Basics
        public virtual bool FX_ProvidesForLayer(FXArgs args)
        {
            if(args.layerTag == "FXTurret")
                return true;
            return false;
        }
        
        public virtual CompPowerTrader FX_PowerProviderFor(FXArgs args) => null!;
        
        //Layer
        public virtual bool? FX_ShouldDraw(FXLayerArgs args) => null;
        public virtual float? FX_GetOpacity(FXLayerArgs args) => null;
        public virtual float? FX_GetRotation(FXLayerArgs args)      
        {
            return args.index switch
            {
                2 => MainGun?.TurretRotation,
                _ => null
            };
        }
        public virtual float? FX_GetRotationSpeedOverride(FXLayerArgs args) => null;
        public virtual float? FX_GetAnimationSpeedFactor(FXLayerArgs args) => null;
        public virtual Color? FX_GetColor(FXLayerArgs args) => null;
        public virtual Vector3? FX_GetDrawPosition(FXLayerArgs args) => null;
        public virtual Func<RoutedDrawArgs, bool> FX_GetDrawFunc(FXLayerArgs args) => null!;

        //Effecters
        public virtual bool? FX_ShouldThrowEffects(FXEffecterArgs args) => true;

        public virtual TargetInfo FX_Effecter_TargetAOverride(FXEffecterArgs args) => null;

        public virtual TargetInfo FX_Effecter_TargetBOverride(FXEffecterArgs args) => null;

        public virtual void FX_OnEffectSpawned(FXEffecterSpawnedEffectEventArgs args)
        {
            
        }
        
        #endregion
        
        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //
            powerComp = GetComp<CompPowerTrader>();
            dormantComp = GetComp<CompCanBeDormant>();
            initiatableComp = GetComp<CompInitiatable>();
            mannableComp = GetComp<CompMannable>();
            refuelComp = GetComp<CompRefuelable>();
            networkComp = GetComp<Comp_Network>();

            defExtension = def.TurretExtension();
            
            turretSet = new TurretGunSet(defExtension, this);
            
            //
            canForceTargetDefault = defExtension.turrets.Any(t => t.canForceTarget);
        }

        public override void Tick()
        {
            base.Tick();
            turretSet.TickTurrets();
        }

        //Basic Turret Functions
        [SyncMethod]
        public sealed override void OrderAttack(LocalTargetInfo targ)
        {
            turretSet.TryOrderAttack(targ);
        }

        [SyncMethod]
        public void ResetOrderedAttack()
        {
            forcedTarget = LocalTargetInfo.Invalid;
            turretSet.ResetOrderedAttack();
            OnResetOrderedAttack();
        }

        protected virtual void OnOrderAttack(LocalTargetInfo targ)
        {

        }

        protected virtual void OnResetOrderedAttack()
        {

        }

        public virtual void Notify_OnProjectileFired()
        {
        }

        public new bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            if (base.ThreatDisabled(disabledFor)) return true;
            if (PowerComp is {PowerOn: false})
            {
                return true;
            }
            return MannableComp is {MannedNow: false};
        }

        //
        public override void Draw()
        {
            base.Draw();
            turretSet.Draw();
            TeleVerb?.DrawVerb();
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                sb.AppendLine(inspectString);
            }

            sb.AppendFormat(turretSet.InspectString());
            return sb.ToString().TrimEndNewlines();
        }

        //
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            foreach (var turretGizmo in turretSet.TurretGizmos())
            {
                yield return turretGizmo;
            }
            
            if (canForceTargetDefault)
            {
                Command_Target targetCommand = new Command_Target
                {
                    defaultLabel = "CommandSetForceAttackTarget".Translate(),
                    defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                    targetingParams = TargetingParameters.ForAttackAny(),
                    action = OrderAttack,
                    hotKey = KeyBindingDefOf.Misc4,
                };
                
                Command_VerbTarget command_VerbTarget = new Command_VerbTarget();
                command_VerbTarget.defaultLabel = "CommandSetForceAttackTarget".Translate();
                command_VerbTarget.defaultDesc = "CommandSetForceAttackTargetDesc".Translate();
                command_VerbTarget.icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true);
                command_VerbTarget.verb = this.AttackVerb;
                command_VerbTarget.hotKey = KeyBindingDefOf.Misc4;
                command_VerbTarget.drawRadius = false;
                if (base.Spawned && true && base.Position.Roofed(base.Map)) //this.IsMortarOrProjectileFliesOverhead 
                {
                    command_VerbTarget.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
                }
                yield return command_VerbTarget;
                
                if (TargetOverride.IsValid)
                {
                    Command_Action command_Action = new Command_Action
                    {
                        defaultLabel = "CommandStopForceAttack".Translate(),
                        defaultDesc = "CommandStopForceAttackDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true),
                        action = delegate()
                        {
                            ResetOrderedAttack();
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        }
                    };
                    if (!TargetOverride.IsValid)
                    {
                        command_Action.Disable("CommandStopAttackFailNotForceAttacking".Translate());
                    }
                    command_Action.hotKey = KeyBindingDefOf.Misc5;
                    yield return command_Action;
                }
                /*
                if (base.Spawned && this.IsMortarOrProjectileFliesOverhead && base.Position.Roofed(base.Map))
                {
                    command_VerbTarget.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
                }
                */
                yield return targetCommand;
            }

            if (DebugSettings.godMode)
            {
            }

            /*
            IEnumerator<Gizmo> enumerator = null;
            CompChangeableProjectile compChangeableProjectile = this.gun.TryGetComp<CompChangeableProjectile>();
            if (compChangeableProjectile != null)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandExtractShell".Translate();
                command_Action.defaultDesc = "CommandExtractShellDesc".Translate();
                command_Action.icon = ContentFinder<Texture2D>.Get("Rimatomics/Things/Resources/sabot/sabot_c", true);
                command_Action.alsoClickIfOtherInGroupClicked = false;
                command_Action.action = new Action(this.dumpShells);
                if (compChangeableProjectile.Projectile == null)
                {
                    command_Action.Disable("NoSabotToExtract".Translate());
                }
                yield return command_Action;
            }
            */
        }
    }
}
