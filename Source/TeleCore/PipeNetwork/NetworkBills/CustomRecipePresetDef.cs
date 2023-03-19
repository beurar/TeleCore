using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TeleCore;

public class CustomRecipePresetDef : Def
{
    [Unsaved] private List<ThingDefCount> cachedResults;
    [Unsaved] private string costLabel;

    public List<string> tags;
    public List<DefIntRef<CustomRecipeRatioDef>> desiredResources;

    public List<ThingDefCount> Results => cachedResults;
    public string CostLabel => costLabel;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        cachedResults = desiredResources.Select(m => new ThingDefCount(m.Def.result, m.Value)).ToList();
        costLabel = NetworkBillUtility.CostLabel(NetworkBillUtility.ConstructCustomCostStack(desiredResources));

        //
        CustomNetworkRecipeReferences.TryRegister(this);
    }
}

