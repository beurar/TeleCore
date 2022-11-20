using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TeleCore
{
    public class JobDriver_EmptyPortableContainer : JobDriver
    {
        //A - Container
        private PortableContainer PortableContainer => TargetA.Thing as PortableContainer;
        //B - Network
        private ThingWithComps NetworkParent => TargetB.Thing as ThingWithComps;

        private NetworkContainer Container => PortableContainer.Container;
        private Comp_NetworkStructure NetworkComp => NetworkParent.GetComp<Comp_NetworkStructure>();
        private NetworkSubPart NetworkPart => NetworkComp[PortableContainer.NetworkDef];
        private NetworkContainer TargetContainer => NetworkPart.Container;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, this.job);
        }

        private JobCondition TransferToContainer()
        {
            if (PortableContainer.Container.Empty) return JobCondition.Succeeded;
            if (TargetContainer.Full) return JobCondition.Incompletable;

            for (int i = Container.AllStoredTypes.Count - 1; i >= 0; i--)
            {
                var type = Container.AllStoredTypes.ElementAt(i);
                if (!NetworkPart.NeedsValue(type, NetworkRole.Storage)) continue;
                if (Container.TryTransferTo(NetworkPart.Container, type, 1, out _))
                {
                    NetworkPart.Notify_ReceivedValue();
                }
            }
            return JobCondition.None;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnBurningImmobile(TargetIndex.A);

            yield return Toils_General.DoAtomic(delegate
            {
                this.job.count = 1;
            });

            Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A, 1, -1, null);
            yield return reserveTargetA;

            //Goto portable container
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            //Start carrying container
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false);

            //Carry container to network structure
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);

            //Start emptying
            Toil emptyContainer = new Toil();
            emptyContainer.tickAction = () =>
            {
                var condition = TransferToContainer();
                if (condition is JobCondition.Succeeded or JobCondition.Incompletable)
                {
                    var thing = PortableContainer;
                    EndJobWith(condition);

                    //
                    thing.Notify_FinishEmptyingToTarget();
                }
            };
            emptyContainer.WithProgressBar(TargetIndex.A, () => PortableContainer.EmptyPercent);
            emptyContainer.defaultCompleteMode = ToilCompleteMode.Never;
            yield return emptyContainer;
        }
    }
}
