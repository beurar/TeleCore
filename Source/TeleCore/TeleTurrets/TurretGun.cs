using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TeleCore
{
    public class TurretGun : IAttackTarget, IAttackTargetSearcher
    {
        //Settings
        private int turretIndex;
        protected TurretProperties props;

        //SubComps
        protected TurretGunTop top;
        protected Effecter progressBarEffecter;

        //Dynamic Data
        private int lastAttackTargetTick;

        private int lastShotIndex = 0;
        private int curShotIndex = 0;
        private int maxShotRotations = 1;

        //
        private LocalTargetInfo lastAttackedTarget;

        protected int burstWarmupTicksLeft;
        protected int burstCooldownTicksLeft;
        protected LocalTargetInfo localForcedTarget = LocalTargetInfo.Invalid;
        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

        public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;
        //
        public LocalTargetInfo CurrentTarget => currentTargetInt;
        public LocalTargetInfo TargetCurrentlyAimingAt => currentTargetInt;

        public Thing Caster => ParentHolder.Caster;
        public Thing ParentThing => ParentHolder.HolderThing;
        //IAttackTarget.Thing | IAttackTargetSearcher.Thing
        public Thing Thing => ParentThing;

        //
        public TurretProperties Props => props;
        public TurretGunSet ParentSet { get; }
        public ITurretHolder ParentHolder { get; }

        //
        public int BurstCoolDownTicksLeft => burstCooldownTicksLeft;
        public int BurstWarmupTicksLeft => burstWarmupTicksLeft;

        protected CompPowerTrader PowerComp => ParentHolder.PowerComp;
        protected CompCanBeDormant DormantComp => ParentHolder.DormantComp;
        protected CompInitiatable InitiatableComp => ParentHolder.InitiatableComp;
        protected CompRefuelable RefuelComp => ParentHolder.RefuelComp;
        protected Comp_NetworkStructure NetworkComp => ParentHolder.NetworkComp;
        protected CompMannable MannableComp => ParentHolder.MannableComp;

        //Basic Turret
        public CompEquippable GunCompEq => Gun.TryGetComp<CompEquippable>();
        private bool WarmingUp => burstWarmupTicksLeft > 0;
        public Verb AttackVerb => GunCompEq.PrimaryVerb;
        public VerbProperties VerbProps => AttackVerb.verbProps;
        public VerbProperties_Extended VerbPropsExtended => AttackVerb.verbProps as VerbProperties_Extended;
        public bool IsMannable => MannableComp != null;
        private bool PlayerControlled => ParentHolder.PlayerControlled;
        private bool CanSetForcedTarget => (MannableComp != null || props.canForceTarget) && PlayerControlled;
        private bool CanToggleHoldFire => PlayerControlled;

        private bool IsMortar => ParentThing.def.building.IsMortar || AttackVerb is Verb_Tele {IsMortar: true};
        private bool IsMortarOrProjectileFliesOverhead => AttackVerb.ProjectileFliesOverhead() || IsMortar;

        private bool CanExtractShell
        {
            get
            {
                if (!PlayerControlled)
                    return false;
                CompChangeableProjectile compChangeableProjectile = Gun.TryGetComp<CompChangeableProjectile>();
                return compChangeableProjectile is { Loaded: true };
            }
        }

        private bool HoldFire => ParentSet.HoldingFire;

        //
        private Pawn ManningPawn => MannableComp?.ManningPawn;
        private bool MannedByColonist => ManningPawn?.Faction == Faction.OfPlayer;

        public Thing Gun { get; private set; }

        //public Verb_Extended AttackVerb => (Verb_Extended)CurrentEffectiveVerb;

        public Verb CurrentEffectiveVerb => GunCompEq.PrimaryVerb;
        public float TurretRotation => top.CurRotation;
        public int LastAttackTargetTick => lastAttackTargetTick;
        public int ShotIndex => curShotIndex;

        public bool Continuous => props.continuous;
        public bool NeedsRoof => IsMortar;

        public TurretGunTop Top => top;
        public bool UsesTurretGunTop => props.turretTop != null;

        public Graphic TurretGraphic => props.turretTop.turret.Graphic;
        public Vector3 DrawPos => ParentThing.DrawPos + props.drawOffset;

        public float TargetPriorityFactor => 1f;

        public TurretGun(TurretProperties props, int index, TurretGunSet set, ITurretHolder parent)
        {
            this.turretIndex = index;
            this.props = props;
            this.ParentSet = set;
            this.ParentHolder = parent;
            //
            burstCooldownTicksLeft = props.turretInitialCooldownTime.SecondsToTicks();
            MakeGun();
            
            //
            if (UsesTurretGunTop)
            {
                top = new TurretGunTop(this, props.turretTop);
                int max1 = 1,
                    max2 = 1;
                if (props.turretTop.barrels != null)
                    max1 = props.turretTop.barrels.Count;
                /*
                if (AttackVerb.Props.originOffsets != null)
                    max2 = AttackVerb.Props.originOffsets.Count;
                */
                maxShotRotations = Math.Max(max1, max2);
            }
        }

        public void Cleanup()
        {
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            progressBarEffecter.Cleanup();
        }

        //
        private void StartTargeting(LocalTargetInfo newTarget)
        {
            ParentSet.Notify_NewTarget(CurrentTarget);
        }

        public void TryOrderAttack(LocalTargetInfo targ)
        {
            if (!targ.IsValid)
            {
                if (localForcedTarget.IsValid)
                {
                    ResetForcedTarget();
                }
                return;
            }
            //Out of Range
            if ((targ.Cell - ParentThing.Position).LengthHorizontal < AttackVerb.verbProps.EffectiveMinRange(targ, Caster))
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), null, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if ((targ.Cell - ParentThing.Position).LengthHorizontal > AttackVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), null, MessageTypeDefOf.RejectInput, false);
                return;
            }

            //
            if (localForcedTarget != targ)
            {
                localForcedTarget = targ;
                if (burstCooldownTicksLeft <= 0)
                    TryStartShootSomething(false);
            }

            //Holding Fire
            if (HoldFire)
            {
                Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(ParentThing.def.label), ParentThing, MessageTypeDefOf.RejectInput, false);
            }
        }

        public void TurretTick()
        {
            //
            if (CanExtractShell && MannedByColonist)
            {
                CompChangeableProjectile compChangeableProjectile = this.Gun.TryGetComp<CompChangeableProjectile>();
                if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
                    ExtractShell();
            }

            //Reset Forced
            if (localForcedTarget.ThingDestroyed || (localForcedTarget.IsValid && !CanSetForcedTarget))
            {
                ResetForcedTarget();
            }

            if (!ParentHolder.IsActive)
            {
                ResetCurrentTarget();
                return;
            }

            //Turret Active
            top?.TurretTopTick();
            GunCompEq.verbTracker.VerbsTick();
            if (!ParentHolder.Stunner.Stunned && AttackVerb.state != VerbState.Bursting)
            {
                if (Continuous)
                {
                    TryStartShootSomething(true);
                }
                else if (WarmingUp)
                {
                    burstWarmupTicksLeft--;
                    if (burstWarmupTicksLeft == 0)
                    {
                        BeginBurst();
                    }
                }
                else
                {
                    if (burstCooldownTicksLeft > 0)
                    {
                        burstCooldownTicksLeft--;
                        if (IsMortar)
                        {
                            if (progressBarEffecter == null)
                            {
                                progressBarEffecter = EffecterDefOf.ProgressBar.Spawn();
                            }
                            progressBarEffecter.EffectTick(ParentThing, TargetInfo.Invalid);
                            MoteProgressBar mote = ((SubEffecter_ProgressBar)this.progressBarEffecter.children[0]).mote;
                            mote.progress = 1f - (float)Math.Max(this.burstCooldownTicksLeft, 0) / (float)this.BurstCooldownTime().SecondsToTicks();
                            mote.offsetZ = -0.8f;
                        }
                    }
                    if (burstCooldownTicksLeft <= 0 && ParentThing.IsHashIntervalTick(VerbPropsExtended?.shotIntervalTicks ?? 10))
                    {
                        TryStartShootSomething(false);
                    }
                }
            }
        }

        protected void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (progressBarEffecter != null)
            {
                progressBarEffecter.Cleanup();
                progressBarEffecter = null;
            }

            if (!ParentThing.Spawned || (HoldFire && CanToggleHoldFire) || (NeedsRoof && ParentThing.Map.roofGrid.Roofed(ParentThing.Position)) || !AttackVerb.Available())
            {
                ResetCurrentTarget();
                return;
            }

            currentTargetInt = localForcedTarget.IsValid ? localForcedTarget : TryFindNewTarget();

            if (CurrentTarget.IsValid)
            {
                StartTargeting(currentTargetInt);

                if (!top?.OnTarget ?? false) return;
                if (canBeginBurstImmediately)
                {
                    BeginBurst();
                }
                else if (props.turretBurstWarmupTime > 0f)
                {
                    burstWarmupTicksLeft = props.turretBurstWarmupTime.SecondsToTicks();

                    //Play Warmup Charge, if available
                    VerbPropsExtended?.chargeSound?.PlayOneShot(SoundInfo.InMap(new TargetInfo(ParentThing)));
                }
            }
            else
            {
                ResetCurrentTarget();
            }
        }

        protected LocalTargetInfo TryFindNewTarget()
        {
            IAttackTargetSearcher attackTargetSearcher = TargSearcher();
            Faction faction = attackTargetSearcher.Thing.Faction;
            float range = AttackVerb.verbProps.range;
            Building t;
            if (Rand.Value < 0.5f && NeedsRoof && faction.HostileTo(Faction.OfPlayer) && ParentThing.Map.listerBuildings.allBuildingsColonist.Where(delegate (Building x)
                {
                    float num = AttackVerb.verbProps.EffectiveMinRange(x, ParentThing);
                    float num2 = x.Position.DistanceToSquared(ParentThing.Position);
                    return num2 > num * num && num2 < range * range;
                }).TryRandomElement(out t))
            {
                return t;
            }
            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
            if (!NeedsRoof)
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
                targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
            }
            if (AttackVerb.IsIncendiary())
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }
            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, IsValidTarget, 0f, 9999f);
        }

        private IAttackTargetSearcher TargSearcher()
        {
            if (MannableComp is {MannedNow: true})
                return MannableComp.ManningPawn;
            return this;
        }

        private bool IsValidTarget(Thing t)
        {
            if (t is not Pawn pawn) return true;
            /*
            if(tiberium.burstMode == TurretBurstMode.ToTarget && tiberium.avoidFriendlyFire)
            {
                ShootLine line = new ShootLine(parent.Position, pawn.Position);
                if(line.Points().Any(P => P.GetFirstBuilding(parent.Map) is Building b && b != parent && b.Faction.IsPlayer))
                {
                    return false;
                }
            }
            */
            if (NeedsRoof)
            {
                RoofDef roofDef = ParentThing.Map.roofGrid.RoofAt(t.Position);
                if (roofDef is {isThickRoof: true})
                {
                    return false;
                }
            }
            if (MannableComp == null)
            {
                return !GenAI.MachinesLike(ParentThing.Faction, pawn);
            }
            /*
            if (ParentHolder.CurrentTarget != null && ParentHolder.CurrentTarget.Thing != t)
                return false;
            if(ParentHolder.HasTarget(t))
            */
            if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }

            //Ignore already chosen targets | this will always choose only one target, needs smarter search
            //if (ParentSet.HasTarget(t)) return false;

            return true;
        }

        protected void BeginBurst()
        {
            AttackVerb.TryStartCastOn(CurrentTarget, false, true);
            OnAttackedTarget(CurrentTarget);
        }

        private void OnAttackedTarget(LocalTargetInfo target)
        {
            lastAttackTargetTick = Find.TickManager.TicksGame;
            lastAttackedTarget = target;
        }

        private void BurstComplete()
        {
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
        }

        public float BurstCooldownTime()
        {
            if (props.turretBurstCooldownTime >= 0f)
            {
                return props.turretBurstCooldownTime;
            }
            return AttackVerb.verbProps.defaultCooldownTime;
        }

        //
        private void MakeGun()
        {
            Gun = ThingMaker.MakeThing(props.turretGunDef);
            UpdateGunVerbs();
        }

        private void UpdateGunVerbs()
        {
            List<Verb> allVerbs = Gun.TryGetComp<CompEquippable>().AllVerbs;
            foreach (var verb in allVerbs)
            {
                verb.caster = ParentHolder.Caster;
                verb.castCompleteCallback = new Action(BurstComplete);
                if (verb is Verb_Tele ve)
                {
                    ve.turretGun = this;
                }
            }
        }

        private void StartShooting()
        {
            if (Continuous)
            {
                //Continuous Shot

            }
            else
            {
                //Burst Shot

            }
        }

        private void ExtractShell()
        {
            GenPlace.TryPlaceThing(this.Gun.TryGetComp<CompChangeableProjectile>().RemoveShell(), ParentThing.Position, ParentThing.Map, ThingPlaceMode.Near, null, null);
        }

        public void ResetForcedTarget()
        {
            ParentSet.Notify_LostTarget(localForcedTarget);
            localForcedTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            if (burstCooldownTicksLeft <= 0)
                TryStartShootSomething(false);
        }

        public void ResetCurrentTarget()
        {
            ParentSet.Notify_LostTarget(currentTargetInt);
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }

        public void Notify_FiredSingleProjectile()
        {
            top?.Notify_TurretShot(curShotIndex);
            RotateNextShotIndex();
            ParentHolder.Notify_OnProjectileFired();
        }

        private void RotateNextShotIndex()
        {
            lastShotIndex = curShotIndex;
            curShotIndex++;
            if (curShotIndex > (maxShotRotations - 1))
                curShotIndex = 0;
        }

        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            return ParentHolder.ThreatDisabled(disabledFor);
        }

        //
        public void Draw()
        {
            if (UsesTurretGunTop)
                top.DrawTurret();
            if (Find.Selector.IsSelected(ParentThing))
                DrawSelectionOverlays();
        }

        private void DrawSelectionOverlays()
        {
            if (localForcedTarget.IsValid && (!localForcedTarget.HasThing || localForcedTarget.Thing.Spawned))
            {
                Vector3 b = localForcedTarget.HasThing ? localForcedTarget.Thing.TrueCenter() : localForcedTarget.CenterVector3;
                Vector3 a = DrawPos;
                b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                a.y = b.y;
                GenDraw.DrawLineBetween(a, b, TeleContent.ForcedTargetLineMat);
            }
            float range = AttackVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(ParentThing.Position, range);
            }
            float num = AttackVerb.verbProps.EffectiveMinRange(true);
            if (num < 90f && num > 0.1f)
            {
                GenDraw.DrawRadiusRing(ParentThing.Position, num);
            }

            if (UsesTurretGunTop && WarmingUp)
            {
                int degreesWide = (int)(burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPieRaw(DrawPos + new Vector3(0f, top.Props.barrelMuzzleOffset.magnitude, 0f), TurretRotation, degreesWide);
            }
        }

        public string GetUniqueLoadID()
        {
            return $"{ParentThing.ThingID}_TurretGun";
        }
    }
}
