using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubMenuGroupDef : Def
{
    [Unsaved] 
    private DesignationTexturePack _textures;
    
    public List<SubMenuCategoryDef> subCategories;
    public string groupIconPath;

    //Pack Def | BuildMenu | Des | Des_Sel | Tab | Tab_Sel
    public string subPackPath;

    public bool isDevGroup = false;
    
    public DesignationTexturePack TexturePack => _textures;

    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(delegate { _textures ??= new DesignationTexturePack(subPackPath, this); });
    }
}