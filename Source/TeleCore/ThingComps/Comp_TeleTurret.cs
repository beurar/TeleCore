using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TeleCore
{
    public class Comp_TeleTurret : ThingComp, ITurretHolder, IAttackTarget, IAttackTargetSearcher
    {
        private CompPowerTrader powerComp;
        private CompCanBeDormant dormantComp;
        private CompInitiatable initiatableComp;
        private CompMannable mannableComp;
        private CompRefuelable refuelComp;
        private Comp_NetworkStructure networkComp;

        //
        private StunHandler stunner;
        private TurretGunSet turretSet;

        //
        public Thing Thing => parent;

        public Verb CurrentEffectiveVerb { get; }
        public LocalTargetInfo LastAttackedTarget { get; }
        public int LastAttackTargetTick { get; }
        public LocalTargetInfo TargetCurrentlyAimingAt { get; }
        public float TargetPriorityFactor { get; }

        public CompProperties_Turret Props => (CompProperties_Turret)base.props;

        //TurretHolder
        public LocalTargetInfo TargetOverride => LocalTargetInfo.Invalid;

        public bool IsActive => parent.Spawned && (PowerComp == null || PowerComp.PowerOn) && (MannableComp == null || MannableComp.MannedNow);
        public bool PlayerControlled => (Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist;

        //
        private Pawn ManningPawn => MannableComp?.ManningPawn;
        private bool MannedByColonist => ManningPawn?.Faction == Faction.OfPlayer;
        private bool MannedByNonColonist => ManningPawn?.Faction != Faction.OfPlayer;

        public Thing Caster => parent;
        public Thing HolderThing => parent;
        public Faction Faction => parent.Faction;
        public CompPowerTrader PowerComp => powerComp;
        public CompCanBeDormant DormantComp => dormantComp;
        public CompInitiatable InitiatableComp => initiatableComp;
        public CompMannable MannableComp => mannableComp;
        public CompRefuelable RefuelComp => refuelComp;
        public Comp_NetworkStructure NetworkComp => networkComp;
        public StunHandler Stunner => stunner;


        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            //
            powerComp = parent.GetComp<CompPowerTrader>();
            dormantComp = parent.GetComp<CompCanBeDormant>();
            initiatableComp = parent.GetComp<CompInitiatable>();
            mannableComp = parent.GetComp<CompMannable>();
            refuelComp = parent.GetComp<CompRefuelable>();
            networkComp = parent.GetComp<Comp_NetworkStructure>();
            
            //
            stunner = new StunHandler(parent);
            turretSet = new TurretGunSet(Props.turrets, this);
        }

        public override void CompTick()
        {
            turretSet.TickTurrets();
        }

        public void Notify_OnProjectileFired()
        {
        }

        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            if (PowerComp is { PowerOn: false })
            {
                return true;
            }
            return MannableComp is { MannedNow: false };
        }

        public override string CompInspectStringExtra()
        {
            return turretSet.InspectString();
        }

        public override void PostDraw()
        {
            turretSet.Draw();
        }

        public string GetUniqueLoadID()
        {
            return parent.GetUniqueLoadID();
        }
    }

    public class CompProperties_Turret : CompProperties
    {
        public List<TurretProperties> turrets;

        public CompProperties_Turret()
        {
            compClass = typeof(Comp_TeleTurret);
        }
    }
}
