using System;
using TeleCore.Data.Events;
using Verse;

namespace TeleCore;

/// <summary>
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
}

/// <summary>
///     Provides an abstract base for custom Thing-tracking worker classes which process Thing-Data on Spawn/Despawn/State
///     change events
/// </summary>
public abstract class ThingTrackerComp
{
    protected ThingTrackerMapInfo parent;
    
    protected ThingTrackerComp(ThingTrackerMapInfo parent)
    {
        this.parent = parent;
        GlobalEventHandler.ThingSpawned += Notify_ThingRegistered;
        GlobalEventHandler.ThingDespawning += Notify_ThingDeregistered;
        GlobalEventHandler.ThingSentSignal += Notify_ThingSentSignal;
    }

    public abstract void Notify_ThingRegistered(ThingStateChangedEventArgs args);
    public abstract void Notify_ThingDeregistered(ThingStateChangedEventArgs args);
    public abstract void Notify_ThingSentSignal(ThingStateChangedEventArgs args);
}