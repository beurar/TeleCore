using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TeleCore.Data.Events;
using TeleCore.Defs;
using TeleCore.Network.Data;
using TeleCore.Network.Utility;
using Verse;

namespace TeleCore.Network;

/// <summary>
/// Creates, Modifies and Destroys PipeNetworks of the same NetworkDef.
/// </summary>
public class PipeSystem
{
    private readonly NetworkDef _def;
    private readonly Map _map;
    
    //
    private readonly List<PipeNetwork> _allNetworks;
    private readonly PipeNetwork?[] _lookUpGrid;
    //private readonly NetworkPartSet _allParts;
    
    //
    private readonly List<DelayedNetworkAction> delayedActions = new();
    
    //Debug
    internal static bool DEBUG_DrawNetwork = false;
    
    public PipeSystem(Map map, NetworkDef networkDef)
    {
        _def = networkDef;
        _map = map;
        _allNetworks = new();
        _lookUpGrid = new PipeNetwork[map.cellIndices.NumGridCells];
        
        //_allParts = new NetworkPartSet(networkDef, null);
        //_allParts.RegisterParentForEvents(this);

        //
        //TODO: Use event subscription
        TFind.TickManager.RegisterMapUITickAction(UpdateNetworkConnections);
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
    }

    public void Notify_PartDespawned(INetworkPart part)
    {
        delayedActions.Add(new DelayedNetworkAction(DelayedNetworkActionType.Deregister, part, part.Thing.Position));
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
        if (cellsByNetwork.ContainsKey(newNetwork))
            cellsByNetwork.Remove(newNetwork);

        var list = new List<IntVec3>();
        cellsByNetwork.Add(newNetwork, list);
        for (int i = 0; i < newNetwork.NetworkCells.Count; i++)
        {
            var cell = newNetwork.NetworkCells[i];
            int num = map.cellIndices.CellToIndex(cell);
            if (lookUpGrid[num] != null)
            {
                if (lookUpGrid[num].Network == newNetwork)
                {
                    TLog.Warning($"Multiple identical cells in NetworkCells list of {newNetwork.Def}: {cell}");
                }
                else
                {
                    TLog.Warning(
                        $"Two Pipe nets on the same cell {cell}: {_lookUpGrid[num].Def}[{_lookUpGrid[num].ID}] instead of {newNetwork.Def}[{newNetwork.ID}]");
                }
            }

            _lookUpGrid[num] = newNetwork;
            list.Add(cell);
            map.mapDrawer.MapMeshDirty(cell,
                MapMeshFlag.Buildings | MapMeshFlag.Things | MapMeshFlag.PowerGrid | MapMeshFlag.Terrain);
        }
    }
    
    private void RegisterNetwork(PipeNetwork newNet) //4.
    {
        _allNetworks.Add(newNet);
        Notify_PipeNetCreated(newNet, newNet);
    }
    
    private void DeregisterNetwork(PipeNetwork oldNet)
    {
        _allNetworks.Remove(oldNet);
        Notify_PipeNetDestroyed(oldNet.Network);
    }
    
    private void TryCreateNetworkAt(IntVec3 cell, Map map, INetworkPart part) //3.
    {
        if (!cell.InBounds(map)) return;
        if (NetworkAt(cell, map) == null)
        {
            PipeNetworkFactory.CreateNetwork(part, out var network);
            RegisterNetwork(network);
        }
    }

    private void TryCreateNetworkAtForDestruction(IntVec3 cell, Map map, INetworkPart destroyedPart)
    {
        if (!cell.InBounds(map)) return;
        if (NetworkAt(cell, map) == null)
        {
            if (PipeNetworkFactory.Fits(cell.GetFirstBuilding(map), destroyedPart.Config.networkDef, out var part))
            {
                PipeNetworkFactory.CreateNetwork(part, out var network);
                RegisterNetwork(network);
            }
        }
    }

    private void TryDestroyNetworkAt(IntVec3 cell, Map map)
    {
        if (!cell.InBounds(map)) return;
        var network = NetworkAt(cell, map);
        if (network != null)
        {
            DeregisterNetwork(network);
        }
    }
    
    //Update Delayed Actions
    private void UpdateNetworkConnections() //2.
    {
        //Update Actions
        if (delayedActions.Count == 0) return;
        var count = delayedActions.Count;

        //Destroy Networks First
        for (var i = 0; i < count; i++)
        {
            DelayedNetworkAction delayedActionForDestruction = delayedActions[i];
            switch (delayedActions[i].type)
            {
                //Should always happen first
                //When registering a new part, first clear the network at the position
                case DelayedNetworkActionType.Register:
                {
                    if (delayedActionForDestruction.pos != delayedActionForDestruction.Part.Thing.Position)
                    {
                        break;
                    }

                    Thing parent = delayedActionForDestruction.Part.Thing;
                    if (NetworkAt(parent.Position, parent.Map) != null)
                    {
                        TLog.Warning($"Tried to register trasmitter {parent} at {parent.Position}, but there is already a power net here. There can't be two transmitters on the same cell.");
                    }

                    //Ensure we only destroy network that we can also connect to
                    foreach (var subPart in delayedActionForDestruction.Part.DirectPartSet.FullSet)
                    {
                        TryDestroyNetworkAt(subPart.Parent.Thing.Position);
                    }

                    break;
                }
                case DelayedNetworkActionType.Deregister:
                {
                    TryDestroyNetworkAt(delayedActionForDestruction.pos);
                    break;
                }
            }
        }

        //Create Networks 
        for (int j = 0; j < count; j++)
        {
            DelayedNetworkAction delayedActionForCreation = delayedActions[j];
            Thing parentThing = delayedActionForCreation.Part.Thing;

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
                    foreach (IntVec3 adjPos in GenAdj.CellsAdjacentCardinal(delayedActionForCreation.pos,
                                 parentThing.Rotation, parentThing.def.size))
                    {
                        TryCreateNetworkAtForDestruction(adjPos, delayedActionForCreation.Part);
                    }

                    break;
                }
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

    public void Tick()
    {
        foreach (var network in _allNetworks)
        {
            network.Tick();
        }
    }

    public void Draw()
    {
        //
        foreach (var network in _allNetworks)
        {
            network.Draw();
        }
        
        //
        if (DEBUG_DrawNetwork)
        {
            for (var i = 0; i < _lookUpGrid.Length; i++)
            {
                var network = _lookUpGrid[i];
                if (network == null) continue;
                CellRenderer.RenderCell(_map.cellIndices.IndexToCell(i), 0.75f);
            }
        }
    }
    
    public void DrawOnGUI()
    {
        foreach (var network in _allNetworks)
        {
            network.OnGUI();
        }
    }
    
    #endregion
    
}