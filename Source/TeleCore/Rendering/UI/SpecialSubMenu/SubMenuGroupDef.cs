using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubMenuGroupDef : Def
{
    public string groupIconPath;

    public bool isDevGroup = false;

    public List<SubMenuCategoryDef> subCategories;

    //Pack Def | BuildMenu | Des | Des_Sel | Tab | Tab_Sel
    public string subPackPath;

    [field: Unsaved] public DesignationTexturePack TexturePack { get; private set; }

    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(
            delegate { TexturePack ??= new DesignationTexturePack(subPackPath, this); });
    }
}