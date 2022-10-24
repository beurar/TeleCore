using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubMenuExtension : DefModExtension
{
    public SubMenuGroupDef groupDef;
    public SubMenuCategoryDef category;
    public bool isDevOption;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }
        if (!groupDef.subCategories.Contains(category))
        {
            yield return $"SubMenuGroupDef '{groupDef.defName}' does not contain any SubMenuCategoryDef '{category.defName}'";
        }
    }
}