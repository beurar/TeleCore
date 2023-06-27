using System.Collections.Generic;
using TeleCore.Generics.Container;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using Verse;
using Verse.AI;

namespace TeleCore;

public class JobDriver_EmptyPortableContainer : JobDriver
{
    //A - Container
    private PortableNetworkContainer PortableNetworkContainer => TargetA.Thing as PortableNetworkContainer;
    //B - Network
    private ThingWithComps NetworkParent => TargetB.Thing as ThingWithComps;

    private FlowBox Container => PortableNetworkContainer.FlowBox;
    private Comp_Network NetworkComp => NetworkParent.GetComp<Comp_Network>();
    private INetworkPart NetworkPart => NetworkComp[PortableNetworkContainer.NetworkDef];
    private FlowBox TargetContainer => NetworkPart.FlowBox;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetA, this.job);
    }

    private JobCondition TransferToContainer()
    {
        if (PortableNetworkContainer.FlowBox.FillState == ContainerFillState.Empty) return JobCondition.Succeeded;
        if (TargetContainer.FillState == ContainerFillState.Full) return JobCondition.Incompletable;

        for (int i = Container.Stack.Length - 1; i >= 0; i--)
        {
            var type = Container.Stack[i];
            //TODO: re-add process
            // if (!NetworkPart.NeedsValue(type, NetworkRole.Storage)) continue;
            // if (Container.TryTransferValue(NetworkPart.Container, type, 1, out _))
            // {
            //     //NetworkPart.Notify_ReceivedValue();
            // }
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
                PortableNetworkContainer.Notify_FinishEmptyingToTarget();
            }
        };
        emptyContainer.WithProgressBar(TargetIndex.A, () => PortableNetworkContainer.EmptyPercent);
        emptyContainer.defaultCompleteMode = ToilCompleteMode.Never;
        yield return emptyContainer;
    }
}