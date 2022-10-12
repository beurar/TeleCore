using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubBuildMenuDef : Def
{
    [Unsaved] 
    private DesignationTexturePack _textures;
    
    public List<SubMenuGroupDef> subMenus;
    public string superPackPath;

    public DesignationTexturePack TexturePack => _textures;
    
    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(delegate { _textures ??= new DesignationTexturePack(superPackPath, this); });
    }
}