using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace TeleCore
{
    public class JobDriver_DoNetworkBill : JobDriver
    {
        public Comp_NetworkBillsCrafter Crafter => job.GetTarget(TargetIndex.A).Thing.TryGetComp<Comp_NetworkBillsCrafter>();

        public CustomNetworkBill CurrentBill => Crafter.BillStack.CurrentBill;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(this.job.GetTarget(TargetIndex.A), this.job))
            {
                return false;
            }
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.InteractionCell);
            var billToil = new Toil();
            billToil.FailOn(() => CurrentBill == null || !CurrentBill.ShouldDoNow());
            billToil.tickAction = delegate
            {
                Pawn actor = billToil.actor;
                Job curJob = actor.jobs.curJob;
                var bill = CurrentBill;
                Pawn pawn = billToil.actor;
                bill.DoWork(pawn);
                if (bill.TryFinish(out var results))
                {
                    //
                    IntVec3 invalid = IntVec3.Invalid;
                    if (bill.StoreMode == BillStoreModeDefOf.BestStockpile)
                    {
                        StoreUtility.TryFindBestBetterStoreCellFor(results[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, out invalid, true);
                    }
                    else if (bill.StoreMode == BillStoreModeDefOf.SpecificStockpile)
                    {
                        StoreUtility.TryFindBestBetterStoreCellForIn(results[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, bill.StoreZone.slotGroup, out invalid, true);
                    }
                    if (invalid.IsValid)
                    {
                        actor.carryTracker.TryStartCarry(results[0]);
                        curJob.targetB = invalid;
                        curJob.targetA = results[0];
                        curJob.count = 99999;
                    }
                    EndJobWith(JobCondition.Succeeded);
                }
            };
            billToil.defaultCompleteMode = ToilCompleteMode.Never;
            billToil.WithEffect(() => EffecterDefOf.ConstructMetal, TargetIndex.A);
            //billToil.PlaySustainerOrSound(() => SoundDefOf.);
            billToil.WithProgressBar(TargetIndex.A, () => 1 - (CurrentBill.WorkLeft / CurrentBill.workAmountTotal), false, -0.5f);
            yield return billToil;

            //
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, storageMode: true, tryStoreInSameStorageIfSpotCantHoldWholeStack: true);
        }
    }
}
