using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class CustomRecipeRatioDef : Def
{
    public List<DefFloatRef<NetworkValueDef>> byProducts;
    public bool hidden = false;
    public List<DefFloatRef<NetworkValueDef>> inputRatio;
    public ThingDef result;
    public List<string> tags;

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        //
        CustomNetworkRecipeReferences.TryRegister(this);
    }
}