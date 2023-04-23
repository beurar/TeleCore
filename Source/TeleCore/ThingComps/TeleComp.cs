using System.Collections.Generic;
using TeleCore.Data.Events;
using Verse;
using Verse.AI;

namespace TeleCore;

public class TeleComp : ThingComp
{
    public TeleDefExtension Extension { get; private set; }
    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Extension = parent.def.TeleExtension();
        if (Extension is {addCustomTick: true} && !parent.IsTeleEntity())
        {
            TeleEventHandler.EntityTicked += TeleTick;
        }
    }

    public override void PostDeSpawn(Map map)
    {
        base.PostDeSpawn(map);
        if (Extension is {addCustomTick: true} && !parent.IsTeleEntity())
        {
            TeleEventHandler.EntityTicked -= TeleTick;
        }
    }

    internal virtual void TeleTick()
    {
        
    }
}