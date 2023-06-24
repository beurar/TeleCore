using System.Collections.Generic;
using TeleCore.Defs;
using Verse;

namespace TeleCore;

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