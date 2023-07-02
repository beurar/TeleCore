using RimWorld;
using Verse;
using Verse.AI;

namespace TeleCore;

public interface ITurretHolder
{
    LocalTargetInfo TargetOverride { get; }
    bool IsActive { get; }
    bool PlayerControlled { get; }

    Thing Caster { get; }
    Thing HolderThing { get; }
    Faction Faction { get; }

    //
    CompPowerTrader PowerComp { get; }
    CompCanBeDormant DormantComp { get; }
    CompInitiatable InitiatableComp { get; }

    CompMannable MannableComp { get; }

    // 
    CompRefuelable RefuelComp { get; }
    Comp_Network NetworkComp { get; }
    StunHandler Stunner { get; }

    void Notify_OnProjectileFired();
    bool ThreatDisabled(IAttackTargetSearcher disabledFor);
}