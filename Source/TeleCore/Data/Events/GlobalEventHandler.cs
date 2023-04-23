using System;
using Verse;

namespace TeleCore.Data.Events;

public static class GlobalEventHandler
{
    public static event ThingSpawnedEvent ThingSpawned;
    public static event ThingDespawnedEvent ThingDespawning;
    public static event ThingStateChangedEvent ThingSentSignal;
    public static event PawnHediffChangedEvent PawnHediffChanged;
    public static event TerrainChangedEvent TerrainChanged;
    public static event CellChangedEvent CellChanged;


    #region Things
    
    internal static void OnThingSpawned(ThingStateChangedEventArgs args)
    {
        try
        {
            ThingSpawned?.Invoke(args);
            CellChanged?.Invoke(new CellChangedEventArgs(args));
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
            CellChanged?.Invoke(new CellChangedEventArgs(args));
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
            CellChanged?.Invoke(new CellChangedEventArgs(args));
        }
        catch (Exception ex)
        {
            TLog.Error($"Error trying to send signal on thing: {args.Thing}\n{ex.Message}");
        }
    }

    #endregion

    #region Terrain

    public static void OnTerrainChanged(TerrainChangedEventArgs args)
    {
        try
        {
            TerrainChanged?.Invoke(args);
            CellChanged?.Invoke(new CellChangedEventArgs(args));
        }
        catch (Exception ex)
        {
            TLog.Error($"Error trying to register terrain change: {args.PreviousTerrain} -> {args.NewTerrain}\n{ex.Message}");
        }
    }

    #endregion
    
    //
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
    
    internal static void ClearData()
    {
        ThingSpawned = null;
        ThingDespawning = null;
        ThingSentSignal = null;
        PawnHediffChanged = null;
    }
}