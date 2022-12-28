using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore;

/// <summary>
/// 
/// </summary>
public class ThingTrackerMapInfo : MapInformation
{
    private readonly ThingTrackerComp[] trackerComps;
    
    public ThingTrackerMapInfo(Map map) : base(map)
    {
        var compTypes = typeof(ThingTrackerComp).AllSubclassesNonAbstract();
        trackerComps = new ThingTrackerComp[compTypes.Count];
        for (var i = 0; i < compTypes.Count; i++)
        {
            var type = compTypes[i];
            try
            {
                trackerComps[i] = (ThingTrackerComp) Activator.CreateInstance(type, this);
            }
            catch (Exception ex)
            {
                TLog.Error($"Could not instantiate {nameof(ThingTrackerComp)} of type {type}:\n{ex}");
            }
        }
    }

    internal void Notify_RegisterThing(Thing thing)
    {
        for (var i = 0; i < trackerComps.Length; i++)
        {
            trackerComps[i].Notify_ThingRegistered(thing);
        }
    }
    
    internal void Notify_DeregisterThing(Thing thing)
    {
        for (var i = 0; i < trackerComps.Length; i++)
        {
            trackerComps[i].Notify_ThingDeregistered(thing);
        }
    }

    internal void Notify_ThingStateChanged(Thing thing, string compSignal = null)
    {
        for (var i = 0; i < trackerComps.Length; i++)
        {
            trackerComps[i].Notify_ThingStateChanged(thing, compSignal);
        }
    }
}

/// <summary>
/// Provides an abstract base for custom Thing-tracking worker classes which process Thing-Data on Spawn/Despawn/State change events
/// </summary>
public abstract class ThingTrackerComp
{
    protected ThingTrackerMapInfo parent;

    //
    protected ThingTrackerComp(ThingTrackerMapInfo parent)
    {
        this.parent = parent;
    }

    public abstract void Notify_ThingRegistered(Thing thing);
    public abstract void Notify_ThingDeregistered(Thing thing);
    public abstract void Notify_ThingStateChanged(Thing thing, string compSignal = null);
}