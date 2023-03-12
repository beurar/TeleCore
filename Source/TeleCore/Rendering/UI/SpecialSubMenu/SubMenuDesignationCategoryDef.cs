using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubMenuDesignationCategoryDef : DesignationCategoryDef
{
    public SubBuildMenuDef menuDef;
    
    public override void ResolveReferences()
    {
        base.ResolveReferences();
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            resolvedDesignators.Clear();
            resolvedDesignators ??= new List<Designator>();
            resolvedDesignators.Add(new Designator_SubBuildMenu(menuDef));
        });
    }
}
