using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore.FlowCore;
using TeleCore.FlowCore.Implementations;
using Verse;
using Verse.AI;

namespace TeleCore
{
    public class JobDriver_EmptyPortableContainer : JobDriver
    {
        //A - Container
        private PortableNetworkContainer PortableNetworkContainerContainer => TargetA.Thing as PortableNetworkContainer;
        //B - Network
        private ThingWithComps NetworkParent => TargetB.Thing as ThingWithComps;

        private NetworkContainerThing<IContainerHolderNetworkThing> Container => PortableNetworkContainerContainer.Container;
        private Comp_NetworkStructure NetworkComp => NetworkParent.GetComp<Comp_NetworkStructure>();
        private NetworkSubPart NetworkPart => NetworkComp[PortableNetworkContainerContainer.NetworkDef];
        private NetworkContainer TargetContainer => NetworkPart.Container;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, this.job);
        }

        private JobCondition TransferToContainer()
        {
            if (PortableNetworkContainerContainer.Container.FillState == ContainerFillState.Empty) return JobCondition.Succeeded;
            if (TargetContainer.FillState == ContainerFillState.Full) return JobCondition.Incompletable;

            for (int i = Container.StoredDefs.Count - 1; i >= 0; i--)
            {
                var type = Container.StoredDefs.ElementAt(i);
                if (!NetworkPart.NeedsValue(type, NetworkRole.Storage)) continue;
                if (Container.TryTransferValue(NetworkPart.Container, type, 1, out _))
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
                    EndJobWith(condition);
                    PortableNetworkContainerContainer.Notify_FinishEmptyingToTarget();
                }
            };
            emptyContainer.WithProgressBar(TargetIndex.A, () => PortableNetworkContainerContainer.EmptyPercent);
            emptyContainer.defaultCompleteMode = ToilCompleteMode.Never;
            yield return emptyContainer;
        }
    }
}
