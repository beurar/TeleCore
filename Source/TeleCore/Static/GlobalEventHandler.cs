using System;

namespace TeleCore.Static;

public static class GlobalEventHandler
{
    public delegate void ThingSpawnedEvent(ThingStateChangedEventArgs args);
    public delegate void ThingDespawnedEvent(ThingStateChangedEventArgs args);
    public delegate void ThingStateChangedEvent(ThingStateChangedEventArgs args);
    
    //
    public static event ThingSpawnedEvent ThingSpawned;
    public static event ThingDespawnedEvent ThingDespawning;
    public static event ThingStateChangedEvent ThingSentSignal;
    
    internal static void OnThingSpawned(ThingStateChangedEventArgs args)
    {
        try
        {
            ThingSpawned?.Invoke(args);
        }
        catch (Exception ex)
        {
            TLog.Error($"Error trying to register spawned thing: {args.Thing}\n{ex.Message}");
        }
    }

    internal static void OnThingDespawning(ThingStateChangedEventArgs args)
    {
        try
        {
            ThingDespawning?.Invoke(args);
        }
        catch (Exception ex)
        {
            TLog.Error($"Error trying to deregister despawned thing: {args.Thing}\n{ex.Message}");
        }
    }

    internal static void OnThingSentSignal(ThingStateChangedEventArgs args)
    {
        try
        {
            ThingSentSignal?.Invoke(args);
        }
        catch (Exception ex)
        {
            TLog.Error($"Error trying to send signal on thing: {args.Thing}\n{ex.Message}");
        }
    }
}