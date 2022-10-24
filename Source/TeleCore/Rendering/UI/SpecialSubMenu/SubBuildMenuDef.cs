using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubBuildMenuDef : Def
{
    [Unsaved] 
    private DesignationTexturePack _textures;
    [Unsaved] 
    private SubMenuAllowWorker _allowWorker;
    
    //
    public List<SubMenuGroupDef> subMenus;
    public string superPackPath;
    public Type allowedDefWorker;
    
    //
    public DesignationTexturePack TexturePack => _textures;
    public SubMenuAllowWorker AllowWorker => _allowWorker;
    
    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            _textures ??= new DesignationTexturePack(superPackPath, this);
            _allowWorker = (SubMenuAllowWorker) Activator.CreateInstance(allowedDefWorker);
        });
    }
}