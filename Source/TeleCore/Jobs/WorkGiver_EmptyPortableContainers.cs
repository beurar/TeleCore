using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.Static;
using Verse;
using Verse.AI;

namespace TeleCore
{
    public class WorkGiver_EmptyPortableContainers : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.ThingGroupCache().ThingsOfGroup(TeleDefOf.NetworkPortableContainers);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return base.ShouldSkip(pawn, forced);
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced)) return false;
            if (t is PortableContainerThing container)
            {
                return container.HasValidTarget;
            }
            JobFailReason.Is($"\n{"TELE.PortableContainer.CannotStartEmptyJob".Translate()}", null);
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            PortableContainerThing container = t as PortableContainerThing;
            var job = new Job(TeleDefOf.EmptyPortableContainer, container, container.TargetToEmptyAt, container.TargetToEmptyAt.Cell);
            job.haulMode = HaulMode.Undefined;
            return job;
        }
    }
}
