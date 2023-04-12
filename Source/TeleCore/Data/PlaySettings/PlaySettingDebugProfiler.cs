using RimWorld;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore;

public class PlaySettingDebugProfiler : PlaySettingsWorker
{
    public override bool Visible => DebugSettings.godMode;
    public override bool DefaultValue => true;
    
    public override Texture2D Icon => TeleContent.KeyFrame;
    public override string Description => "";

    public override void OnToggled(bool isActive)
    {
        if (!isActive)
        {
            TProfiler.Disable();
        }
        else
        { 
            TProfiler.Enable();
        }
    }
}