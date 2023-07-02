using System.Collections.Generic;
using Verse;

namespace TeleCore.FlowCore;

public class FlowValueCollectionDef : Def
{
    [field: Unsaved] public List<FlowValueDef> ValueDefs { get; } = new();

    public void Notify_ResolvedFlowValueDef(FlowValueDef def)
    {
        ValueDefs.Add(def);
    }
}