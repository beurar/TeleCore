using System.Collections.Generic;
using System.Linq;
using TeleCore.Network.Utility;
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
    public bool HasByProducts => desiredResources.Any(r => r.Def.byProducts != null);

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        cachedResults = desiredResources.Select(m => new ThingDefCount(m.Def.result, m.Value)).ToList();
        costLabel = NetworkBillUtility.CostLabel(NetworkBillUtility.ConstructCustomCostStack(desiredResources));

        //
        CustomNetworkRecipeReferences.TryRegister(this);
    }
}