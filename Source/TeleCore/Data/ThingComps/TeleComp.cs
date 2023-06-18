using System.Collections.Generic;
using TeleCore.Data.Events;
using Verse;
using Verse.AI;

namespace TeleCore;

/// <summary>
/// Base class for TeleCore ThingComps.
/// </summary>
public abstract class TeleComp : ThingComp
{
    public TeleDefExtension Extension { get; private set; }
    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (parent.def.HasTeleExtension(out var textension))
        {
            Extension = textension;
            if (Extension.addCustomTick)
            {
                TeleEventHandler.EntityTicked += TeleTick;
            }
        }
    }

    public override void PostDeSpawn(Map map)
    {
        base.PostDeSpawn(map);
        if (Extension != null)
        {
            if (Extension.addCustomTick && !parent.IsTeleEntity())
            {
                TeleEventHandler.EntityTicked -= TeleTick;
            }
        }
    }

    internal virtual void TeleTick()
    {
        
    }
}