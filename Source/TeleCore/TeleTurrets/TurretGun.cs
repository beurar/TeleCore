using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TeleCore;

public class TurretGun : IAttackTarget, IAttackTargetSearcher
{
    protected int burstCooldownTicksLeft;

    protected int burstWarmupTicksLeft;
    protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

    //

    //Dynamic Data

    private int lastShotIndex;
    protected LocalTargetInfo localForcedTarget = LocalTargetInfo.Invalid;
    private int maxShotRotations = 1;
    protected Effecter progressBarEffecter;
    protected TurretProperties props;

    //SubComps
    protected TurretGunTop top;

    //Settings
    private int turretIndex;

    //
    public LocalTargetInfo CurrentTarget => currentTargetInt;

    public Thing Caster => ParentHolder.Caster;
    public Thing ParentThing => ParentHolder.HolderThing;

    //
    public TurretProperties Props => props;
    public TurretGunSet ParentSet { get; private set; }
    public ITurretHolder ParentHolder { get; private set; }

    //
    public int BurstCoolDownTicksLeft => burstCooldownTicksLeft;
    public int BurstWarmupTicksLeft => burstWarmupTicksLeft;

    public CompPowerTrader PowerComp => ParentHolder.PowerComp;
    public CompCanBeDormant DormantComp => ParentHolder.DormantComp;
    public CompInitiatable InitiatableComp => ParentHolder.InitiatableComp;
    public CompRefuelable RefuelComp => ParentHolder.RefuelComp;
    public Comp_Network NetworkComp => ParentHolder.NetworkComp;
    public CompMannable MannableComp => ParentHolder.MannableComp;

    //Basic Turret
    public CompEquippable GunCompEq => Gun.TryGetComp<CompEquippable>();
    private bool WarmingUp => burstWarmupTicksLeft > 0;
    public Verb AttackVerb => GunCompEq.PrimaryVerb;
    public VerbProperties VerbProps => AttackVerb.verbProps;
    public Verb_Tele TeleVerb => AttackVerb as Verb_Tele;
    public VerbProperties_Extended VerbPropsExtended => TeleVerb.Props;
    public bool IsMannable => MannableComp != null;
    public bool PlayerControlled => ParentHolder.PlayerControlled;
    public bool CanSetForcedTarget => (MannableComp != null || props.canForceTarget) && PlayerControlled;
    public bool CanToggleHoldFire => PlayerControlled;

    private bool IsMortar => ParentThing.def.building.IsMortar || AttackVerb is Verb_Tele {IsMortar: true};
    private bool IsMortarOrProjectileFliesOverhead => AttackVerb.ProjectileFliesOverhead() || IsMortar;

    private bool CanExtractShell
    {
        get
        {
            if (!PlayerControlled)
                return false;
            var compChangeableProjectile = Gun.TryGetComp<CompChangeableProjectile>();
            return compChangeableProjectile is {Loaded: true};
        }
    }

    private bool HoldFire => ParentSet.HoldingFire;

    //
    private Pawn ManningPawn => MannableComp?.ManningPawn;
    private bool MannedByColonist => ManningPawn?.Faction == Faction.OfPlayer;

    public Thing Gun { get; private set; }
    public float TurretRotation => top?.CurRotation ?? 0;
    public int ShotIndex { get; private set; }

    public bool Continuous => props.continuous;
    public bool NeedsRoof => IsMortar;

    public TurretGunTop Top => top;
    public bool UsesTurretGunTop => props.turretTop != null;

    public Graphic TurretGraphic => props.turretTop.topGraphic.Graphic;
    public Vector3 DrawPos => ParentThing.DrawPos + props.turretOffset;

    public LocalTargetInfo TargetCurrentlyAimingAt => currentTargetInt;

    //IAttackTarget.Thing | IAttackTargetSearcher.Thing
    public Thing Thing => ParentThing;

    public float TargetPriorityFactor => 1f;

    public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
    {
        return ParentHolder.ThreatDisabled(disabledFor);
    }

    public string GetUniqueLoadID()
    {
        return $"{ParentThing.ThingID}_TurretGun";
    }

    public LocalTargetInfo LastAttackedTarget { get; private set; }

    //public Verb_Extended AttackVerb => (Verb_Extended)CurrentEffectiveVerb;

    public Verb CurrentEffectiveVerb => GunCompEq.PrimaryVerb;
    public int LastAttackTargetTick { get; private set; }

    internal void Setup(TurretProperties props, int index, TurretGunSet set, ITurretHolder parent)
    {
        turretIndex = index;
        this.props = props;
        ParentSet = set;
        ParentHolder = parent;
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
            if (localForcedTarget.IsValid) ResetForcedTarget();
            return;
        }

        if (!CanSetForcedTarget)
        {
            Messages.Message("Tele.TurretGunCantSetForced".Translate(), null, MessageTypeDefOf.RejectInput, false);
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
            Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(ParentThing.def.label), ParentThing,
                MessageTypeDefOf.RejectInput, false);
            ResetForcedTarget();
        }
    }

    public void TurretTick()
    {
        //
        if (CanExtractShell && MannedByColonist)
        {
            var compChangeableProjectile = Gun.TryGetComp<CompChangeableProjectile>();
            if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
                ExtractShell();
        }

        //Reset Forced
        if (localForcedTarget.ThingDestroyed || (localForcedTarget.IsValid && !CanSetForcedTarget)) ResetForcedTarget();

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
                if (burstWarmupTicksLeft == props.turretBurstWarmupTime.SecondsToTicks() - 1)
                    //Play Warmup Charge, if available
                    VerbPropsExtended?.chargeSound?.PlayOneShot(SoundInfo.InMap(new TargetInfo(ParentThing)));

                burstWarmupTicksLeft--;
                if (burstWarmupTicksLeft == 0) BeginBurst();
            }
            else
            {
                if (burstCooldownTicksLeft > 0)
                {
                    burstCooldownTicksLeft--;
                    if (IsMortar)
                    {
                        if (progressBarEffecter == null) progressBarEffecter = EffecterDefOf.ProgressBar.Spawn();
                        progressBarEffecter.EffectTick(ParentThing, TargetInfo.Invalid);
                        var mote = ((SubEffecter_ProgressBar) progressBarEffecter.children[0]).mote;
                        mote.progress = 1f - Math.Max(burstCooldownTicksLeft, 0) /
                            (float) BurstCooldownTime().SecondsToTicks();
                        mote.offsetZ = -0.8f;
                    }
                }

                if (burstCooldownTicksLeft <= 0 &&
                    ParentThing.IsHashIntervalTick(VerbPropsExtended?.shotIntervalTicks ?? 10))
                    TryStartShootSomething(false);
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

        if (!ParentThing.Spawned || (HoldFire && CanToggleHoldFire) ||
            (NeedsRoof && ParentThing.Map.roofGrid.Roofed(ParentThing.Position)) || !AttackVerb.Available())
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
                BeginBurst();
            else if (props.turretBurstWarmupTime > 0f)
                burstWarmupTicksLeft = props.turretBurstWarmupTime.SecondsToTicks();
        }
        else
        {
            ResetCurrentTarget();
        }
    }

    protected LocalTargetInfo TryFindNewTarget()
    {
        var attackTargetSearcher = TargSearcher();
        var faction = attackTargetSearcher.Thing.Faction;
        var range = AttackVerb.verbProps.range;
        Building t;
        if (Rand.Value < 0.5f && NeedsRoof && faction.HostileTo(Faction.OfPlayer) && ParentThing.Map.listerBuildings
                .allBuildingsColonist.Where(delegate(Building x)
                {
                    var num = AttackVerb.verbProps.EffectiveMinRange(x, ParentThing);
                    float num2 = x.Position.DistanceToSquared(ParentThing.Position);
                    return num2 > num * num && num2 < range * range;
                }).TryRandomElement(out t))
            return t;
        var targetScanFlags = TargetScanFlags.NeedThreat;
        if (!NeedsRoof)
        {
            targetScanFlags |= TargetScanFlags.NeedLOSToAll;
            targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
        }

        if (AttackVerb.IsIncendiary()) targetScanFlags |= TargetScanFlags.NeedNonBurning;
        return (Thing) AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags,
            IsValidTarget);
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
            var roofDef = ParentThing.Map.roofGrid.RoofAt(t.Position);
            if (roofDef is {isThickRoof: true}) return false;
        }

        if (MannableComp == null) return !GenAI.MachinesLike(ParentThing.Faction, pawn);
        /*
        if (ParentHolder.CurrentTarget != null && ParentHolder.CurrentTarget.Thing != t)
            return false;
        if(ParentHolder.HasTarget(t))
        */
        if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer) return false;

        //Ignore already chosen targets | this will always choose only one target, needs smarter search
        //if (ParentSet.HasTarget(t)) return false;

        return true;
    }

    protected void BeginBurst()
    {
        AttackVerb.TryStartCastOn(CurrentTarget);
        OnAttackedTarget(CurrentTarget);
    }

    private void OnAttackedTarget(LocalTargetInfo target)
    {
        LastAttackTargetTick = Find.TickManager.TicksGame;
        LastAttackedTarget = target;
    }

    private void BurstComplete()
    {
        burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
    }

    public float BurstCooldownTime()
    {
        if (props.turretBurstCooldownTime >= 0f) return props.turretBurstCooldownTime;
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
            verb.castCompleteCallback = BurstComplete;
            if (verb is Verb_Tele ve) ve.turretGun = this;
        }
    }

    private void StartShooting()
    {
        if (Continuous)
        {
            //Continuous Shot
        }
        //Burst Shot
    }

    private void ExtractShell()
    {
        GenPlace.TryPlaceThing(Gun.TryGetComp<CompChangeableProjectile>().RemoveShell(), ParentThing.Position,
            ParentThing.Map, ThingPlaceMode.Near);
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
        top?.Notify_TurretShot(ShotIndex);
        RotateNextShotIndex();
        ParentHolder.Notify_OnProjectileFired();
    }

    private void RotateNextShotIndex()
    {
        lastShotIndex = ShotIndex;
        ShotIndex++;
        if (ShotIndex > maxShotRotations - 1)
            ShotIndex = 0;
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
            var b = localForcedTarget.HasThing ? localForcedTarget.Thing.TrueCenter() : localForcedTarget.CenterVector3;
            var a = DrawPos;
            b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            a.y = b.y;
            GenDraw.DrawLineBetween(a, b, TeleContent.ForcedTargetLineMat);
        }

        var range = AttackVerb.verbProps.range;
        if (range < 90f) GenDraw.DrawRadiusRing(ParentThing.Position, range);
        var num = AttackVerb.verbProps.EffectiveMinRange(true);
        if (num < 90f && num > 0.1f) GenDraw.DrawRadiusRing(ParentThing.Position, num);

        if (UsesTurretGunTop && WarmingUp)
        {
            var degreesWide = (int) (burstWarmupTicksLeft * 0.5f);
            GenDraw.DrawAimPieRaw(DrawPos + (TeleVerb?.BaseDrawOffsetRotated ?? Vector3.zero), TurretRotation,
                degreesWide);
        }
    }
}