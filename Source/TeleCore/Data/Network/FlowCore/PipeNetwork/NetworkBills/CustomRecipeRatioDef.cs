using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    public class CustomRecipeRatioDef : Def
    {
        public bool hidden = false;
        public List<string> tags;
        public List<DefFloatRef<NetworkValueDef>> inputRatio;
        public List<DefFloatRef<NetworkValueDef>> byProducts;
        public ThingDef result;

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            //
            CustomNetworkRecipeReferences.TryRegister(this);
        }
    }
}
