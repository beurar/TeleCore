using RimWorld;
using TeleCore.Defs;
using TeleCore.Network.Flow;
using Verse;

namespace TeleCore.Network.Data;

public class PortableNetworkContainer : FXThing
{
    //Portable Data
    private NetworkDef networkDef;
        
    //Targeting
    private TargetingParameters? paramsInt;
    private LocalTargetInfo currentDesignatedTarget = LocalTargetInfo.Invalid;
        
    //
    public NetworkDef NetworkDef => networkDef;
    public FlowBox FlowBox { get; private set; }
    
    public float EmptyPercent => (float)FlowBox.FillPercent - 1f;
    public bool HasValidTarget { get; set; }
    public LocalTargetInfo TargetToEmptyAt { get; set; }

    public void Notify_FinishEmptyingToTarget()
    {
        throw new System.NotImplementedException();
    }
}