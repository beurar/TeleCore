using System.Collections.Generic;
using Verse;

namespace TeleCore.FlowCore;

public class FlowValueCollectionDef<TValue> : Def
where TValue : FlowValueDef<TValue>
{
    [field: Unsaved] 
    public List<FlowValueDef<TValue> > ValueDefs { get; } = new();

    public void Notify_ResolvedFlowValueDef(FlowValueDef<TValue> def)
    {
        ValueDefs.Add(def);
    }
}