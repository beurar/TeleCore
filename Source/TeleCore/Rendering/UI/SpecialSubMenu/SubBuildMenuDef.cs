using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubBuildMenuDef : Def
{
    [Unsaved] 
    private DesignationTexturePack _textures;
    [Unsaved] 
    private SubMenuVisibilityWorker _visWorker;
    
    //
    public List<SubMenuGroupDef> subMenus;
    public string superPackPath;
    public Type visWorker = typeof(SubMenuVisibilityWorker);
    
    //
    public DesignationTexturePack TexturePack => _textures;
    public SubMenuVisibilityWorker VisWorker => _visWorker;
    
    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            _textures ??= new DesignationTexturePack(superPackPath, this);
            _visWorker = (SubMenuVisibilityWorker) Activator.CreateInstance(visWorker);
        });
    }
}