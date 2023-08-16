using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TeleCore.Data.Events;
using TeleCore.Network.Data;
using TeleCore.Network.Utility;
using UnityEngine;
using Verse;

namespace TeleCore.Network;

/// <summary>
///     Creates, Modifies and Destroys PipeNetworks of the same NetworkDef.
/// </summary>
public class PipeNetworkMaster
{
    //Debug
    internal static bool DEBUG_DrawNetwork = true;

    private readonly NetworkPartSet _totalPartSet;
    private readonly List<PipeNetwork> _allNetworks;
    private readonly NetworkDef _def;
    private readonly PipeNetwork?[] _lookUpGrid;

    private readonly Map _map;
    
    private readonly List<DelayedNetworkAction> delayedActions = new();

    public NetworkPartSet TotalPartSet => _totalPartSet;
    
    public PipeNetworkMaster(Map map, NetworkDef networkDef)
    {
        _def = networkDef;
        _map = map;
        _allNetworks = new List<PipeNetwork>();
        _lookUpGrid = new PipeNetwork[map.cellIndices.NumGridCells];
        _totalPartSet = new NetworkPartSet(networkDef);
        //_allParts.RegisterParentForEvents(this);

        //
        //TODO: Use event subscription
        TFind.TickManager.RegisterMapUITickAction(TickUpdate);
    }

    #region Data Getters

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PipeNetwork? NetworkAt(IntVec3 c, Map map)
    {
        return _lookUpGrid[c.Index(map)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool? HasNetworkConnectionAt(IntVec3 c, Map map)
    {
        return _lookUpGrid[c.Index(map)] != null;
    }

    #endregion

    #region Generation

    //Network Notifiers
    public void Notify_PartSpawned(INetworkPart part) //1.
    {
        delayedActions.Add(new DelayedNetworkAction(DelayedNetworkActionType.Register, part, part.Thing.Position));
        
        //Add Part
        _totalPartSet.AddComponent(part);
    }

    public void Notify_PartDespawned(INetworkPart part)
    {
        delayedActions.Add(new DelayedNetworkAction(DelayedNetworkActionType.Deregister, part, part.Thing.Position));
        
        //Remove Part
        _totalPartSet.RemoveComponent(part);
    }

    //Notify Spawned 1.
    //Delay 2.
    //IF CREATE
    //Destroy old and make new 3.
    //Try create new 4.
    //Register new
    //Create new from factory with graph 
    //Notify new network created and update a bunch of shit 5.

    public void Notify_PipeNetCreated(PipeNetwork newNetwork) //5.
    {
        var graph = newNetwork.Graph;
        var graphCells = newNetwork.Graph.Cells;
        if (graphCells.NullOrEmpty())
        {
            TLog.Warning("Tried to generate system from empty graph.");
            return;
        }
        for (var i = 0; i < graphCells.Count; i++)
        {
            var cell = graphCells[i];
            var num = _map.cellIndices.CellToIndex(cell);
            if (_lookUpGrid[num] != null)
            {
                if (_lookUpGrid[num] == newNetwork)
                    TLog.Warning($"Multiple identical cells in NetworkCells list of {newNetwork.NetworkDef}: {cell}");
                else
                    TLog.Warning(
                        $"Two Pipe nets on the same cell {cell}: {_lookUpGrid[num].NetworkDef}[{_lookUpGrid[num].ID}] instead of {newNetwork.NetworkDef}[{newNetwork.ID}]");
            }

            _lookUpGrid[num] = newNetwork;
            _map.mapDrawer.MapMeshDirty(cell,MapMeshFlag.Buildings | MapMeshFlag.Things | MapMeshFlag.PowerGrid | MapMeshFlag.Terrain);
        }
    }

    public void Notify_PipeNetDestroyed(PipeNetwork deadNetwork)
    {
        var list = deadNetwork.Graph.Cells;

        //
        for (var i = 0; i < list.Count; i++)
        {
            var num = _map.cellIndices.CellToIndex(list[i]);
            if (_lookUpGrid[num] == deadNetwork)
                _lookUpGrid[num] = null;
            else if (_lookUpGrid[num] != null)
                TLog.Warning($"Multiple networks on the same cell {list[i]}. This is probably a result of an earlier error.");
        }

        //OnNetworkDestroyed();
        foreach (var networkSubPart in deadNetwork.PartSet.FullSet)
            networkSubPart.Network = null;

        deadNetwork.Dispose();
    }

    private void RegisterNetwork(PipeNetwork newNet) //4.
    {
        _allNetworks.Add(newNet);
        Notify_PipeNetCreated(newNet);
    }

    private void DeregisterNetwork(PipeNetwork oldNet)
    {
        _allNetworks.Remove(oldNet);
        Notify_PipeNetDestroyed(oldNet);
    }

    private void TryCreateNetworkAt(IntVec3 cell, INetworkPart part) //3.
    {
        if (!cell.InBounds(_map)) return;
        if (NetworkAt(cell, _map) != null) return;
        
        PipeNetworkFactory.CreateNetwork(this, part, out var network);
        RegisterNetwork(network);
    }

    private void TryCreateNetworkAtForDestruction(IntVec3 cell, INetworkPart destroyedPart)
    {
        if (!cell.InBounds(_map)) return;
        if (NetworkAt(cell, _map) != null) return;
        
        if (PipeNetworkFactory.Fits(cell.GetFirstBuilding(_map), destroyedPart.Config.networkDef, out var part))
        {
            PipeNetworkFactory.CreateNetwork(this, part!, out var network);
            RegisterNetwork(network);
        }
    }

    private void TryDestroyNetworkAt(IntVec3 cell)
    {
        if (!cell.InBounds(_map)) return;
        var network = NetworkAt(cell, _map);
        if (network != null) DeregisterNetwork(network);
    }

    //Update Delayed Actions
    private void TickUpdate() //2.
    {
        //Update Actions
        if (delayedActions.Count == 0) return;
        var count = delayedActions.Count;

        //Destroy Networks First
        for (var i = 0; i < count; i++)
        {
            var delayedActionForDestruction = delayedActions[i];
            try
            {
                switch (delayedActions[i].type)
                {
                    //Should always happen first
                    //When registering a new part, first clear the network at the position
                    case DelayedNetworkActionType.Register:
                    {
                        if (delayedActionForDestruction.pos != delayedActionForDestruction.Part.Thing.Position) break;

                        var parent = delayedActionForDestruction.Part.Thing;
                        if (NetworkAt(parent.Position, parent.Map) != null)
                            TLog.Warning(
                                $"Tried to register trasmitter {parent} at {parent.Position}, but there is already a power net here. There can't be two transmitters on the same cell.");

                        //Ensure we only destroy network that we can also connect to
                        foreach (var subPart in delayedActionForDestruction.Part.AdjacentSet.FullSet)
                            TryDestroyNetworkAt(subPart.Parent.Thing.Position);

                        break;
                    }
                    case DelayedNetworkActionType.Deregister:
                    {
                        TryDestroyNetworkAt(delayedActionForDestruction.pos);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                TLog.Error(e.Message + "\n" + e.StackTrace);
                delayedActions.RemoveAt(i);
                continue;
            }
        }

        //Create Networks 
        for (var j = 0; j < count; j++)
        {
            var delayedActionForCreation = delayedActions[j];
            var parentThing = delayedActionForCreation.Part.Thing;
            try
            {
                switch (delayedActions[j].type)
                {
                    //Create On Newly Spawned Comp
                    case DelayedNetworkActionType.Register:
                    {
                        TryCreateNetworkAt(delayedActionForCreation.pos, delayedActionForCreation.Part);
                        break;
                    }
                    //Create By Checking Adjacent Cells Of Despawned Component
                    case DelayedNetworkActionType.Deregister:
                    {
                        foreach (var adjPos in GenAdj.CellsAdjacentCardinal(delayedActionForCreation.pos,
                                     parentThing.Rotation, parentThing.def.size))
                            TryCreateNetworkAtForDestruction(adjPos, delayedActionForCreation.Part);
                        break;
                    }
                }
            }           
            catch (Exception e)
            {
                TLog.Error(e.Message + "\n" + e.StackTrace);
                delayedActions.RemoveAt(j);
                continue;
            }
        }

        delayedActions.RemoveRange(0, count);
    }

    #endregion

    /*private void OnNetworkDestroyed()
    {
        NetworkDestroyed(new NetworkChangedEventArgs(NetworkChangeType.Destroyed));
    }

    //Single Parts
    public void Notify_AddPart(INetworkPart part)
    {
        //TotalPartSet.AddNewComponent(part);
        AddedPart(new NetworkChangedEventArgs(NetworkChangeType.AddedPart, part));
    }

    public void Notify_RemovePart(INetworkPart part)
    {
        //TotalPartSet.RemoveComponent(part);
        RemovedPart(new NetworkChangedEventArgs(NetworkChangeType.RemovedPart, part));
    }*/

    #region TickUpdates
    
    public void Tick(bool shouldTick, int tick)
    {
        foreach (var network in _allNetworks)
        {
            network.TickSystem(tick);
            if(shouldTick)
                network.Tick(tick);
        }
    }

    public void Draw()
    {
        //
        foreach (var network in _allNetworks)
            network.Draw();

        //
        if (DEBUG_DrawNetwork)
        {
            // for (var i = 0; i < _lookUpGrid.Length; i++)
            // {
            //     var network = _lookUpGrid[i];
            //     if (network == null) continue;
            //     CellRenderer.RenderCell(_map.cellIndices.IndexToCell(i), network.GetHashCode()/10000f);
            // }
        }
    }

    public void DrawOnGUI()
    {
        foreach (var network in _allNetworks) 
            network.OnGUI();
    }

    #endregion
}