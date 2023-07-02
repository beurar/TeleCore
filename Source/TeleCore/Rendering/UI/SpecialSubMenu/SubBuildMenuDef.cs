using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class SubBuildMenuDef : Def
{
    //
    public List<SubMenuGroupDef> subMenus;
    public string superPackPath;
    public Type visWorker = typeof(SubMenuVisibilityWorker);

    //
    [field: Unsaved] public DesignationTexturePack TexturePack { get; private set; }

    [field: Unsaved] public SubMenuVisibilityWorker VisWorker { get; private set; }

    public override void PostLoad()
    {
        base.PostLoad();
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            TexturePack ??= new DesignationTexturePack(superPackPath, this);
            VisWorker = (SubMenuVisibilityWorker) Activator.CreateInstance(visWorker);
        });
    }
}