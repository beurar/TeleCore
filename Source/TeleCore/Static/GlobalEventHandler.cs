using System;

namespace TeleCore.Static;

public static class GlobalEventHandler
{
    public static event ThingSpawnedEvent ThingSpawned;
    public static event ThingDespawnedEvent ThingDespawning;
    public static event ThingStateChangedEvent ThingSentSignal;
    public static event PawnHediffChangedEvent PawnHediffChanged;
    
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

    internal static void OnPawnHediffChanged(PawnHediffChangedEventArgs args)
    {
        try
        {
            PawnHediffChanged?.Invoke(args);
        }
        catch (Exception ex)
        {
            TLog.Error($"Error trying to register hediff change on pawn: {args.Pawn}\n{ex.Message}");
        }
    }
}