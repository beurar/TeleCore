using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class FlowValueCollectionDef : Def
{
    [field: Unsaved] 
    public List<FlowValueDef> ValueDefs { get; } = new();

    public void Notify_ResolvedFlowValueDef(FlowValueDef def)
    {
        ValueDefs.Add(def);
    }

    public override void ResolveReferences()
    {
        foreach (var valueDef in ValueDefs)
        {
            valueDef.collectionDef = this;
        }
    }
}