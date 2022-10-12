using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubMenuDesignator : DesignationCategoryDef
{
    public SubBuildMenuDef menuDef;
    //public DesignationTexturePack texturePack;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            resolvedDesignators.Clear();
            resolvedDesignators ??= new List<Designator>();
            resolvedDesignators.Add(new Designator_SubBuildMenu(menuDef));
            TLog.Message($"Added custom SubBuildMenu designator for {menuDef}");
        });
    }
}
