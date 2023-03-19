using System.Collections.Generic;
using Verse;

namespace TeleCore.FlowCore;

public class FlowValueCollectionDef : Def
{
    [Unsaved]
    private readonly List<FlowValueDef> resolvedValueDefs = new();

    public List<FlowValueDef> ValueDefs => resolvedValueDefs;
    
    public FlowValueCollectionDef()
    {
    }

    public void Notify_ResolvedFlowValueDef(FlowValueDef def)
    {
        resolvedValueDefs.Add(def);
    }
}