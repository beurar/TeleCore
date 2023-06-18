using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TeleCore.Data.Events;
using TeleCore.Data.Network.Generation;
using TeleCore.Network.Generation;
using TeleCore.Network.PressureSystem;
using Verse;

namespace TeleCore.Network;

/// <summary>
/// A system containing all the networks of a single type on a single map.
/// Also handles creating and destroying networks
/// </summary>
public class PipeNetworkSystem
{
    internal static int MasterID = 0;

    private readonly Map map;
    private readonly NetworkComplex[] lookUpGrid;

    //
    private readonly List<NetworkComplex> allComplexes;
    private readonly List<PipeNetwork> allNetworks;
    private Dictionary<PipeNetwork, List<IntVec3>> cellsByNetwork;

    //
    private readonly List<DelayedNetworkAction> delayedActions = new();

    public NetworkPartSet TotalPartSet { get; private set; }

    //Debug
    internal static bool DEBUG_DrawNetwork = false;

    public NetworkDef NetworkDef { get; }
    public Map Map => map;

    public NetworkSubPart MainNetworkPart { get; private set; }

    public List<PipeNetwork> AllNetworks => allNetworks;

    public PipeNetworkSystem(Map map, NetworkDef networkDef)
    {
        this.map = map;
        NetworkDef = networkDef;
        TotalPartSet = new NetworkPartSet(networkDef, null);
        TotalPartSet.RegisterParentForEvents(this);
        lookUpGrid = new NetworkComplex[map.cellIndices.NumGridCells];

        allNetworks = new();
        allComplexes = new();
        cellsByNetwork = new();

        //
        //TODO: Use event subscription
        TFind.TickManager.RegisterMapUITickAction(UpdateNetworkConnections);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NetworkComplex ComplexAt(IntVec3 c)
    {
        return lookUpGrid[c.Index(map)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasNetworkConnectionAt(IntVec3 c)
    {
        return lookUpGrid[c.Index(map)] != null;
    }
    
    public void DEBUG_ToggleShowNetworks()
    {
        DEBUG_DrawNetwork = !DEBUG_DrawNetwork;
    }

    public void TickNetworks()
    {
        foreach (var complex in allComplexes)
        {
            complex.Tick();
        }
    }

    public void DrawNetwork()
    {
        //
        foreach (var network in allNetworks)
        {
            network.Draw();
            if (DEBUG_DrawNetwork)
            {
                for (var c = 0; c < network.NetworkCells.Count; c++)
                {
                    CellRenderer.RenderCell(network.NetworkCells[c], 0.75f);
                }
            }
        }
    }

    public void DrawNetworkOnGUI()
    {
        foreach (var network in allNetworks)
        {
            network.DrawOnGUI();
        }
    }

    //Network Components
    public void RegisterComponent(NetworkSubPart part, Comp_Network netComp)
    {
        delayedActions.Add(new DelayedNetworkAction(DelayedNetworkActionType.Register, part, part.Thing.Position));
    }

    public void DeregisterComponent(NetworkSubPart part, Comp_Network netComp)
    {
        delayedActions.Add(new DelayedNetworkAction(DelayedNetworkActionType.Deregister, part, part.Thing.Position));
    }

    //Update Delayed Actions
    public void UpdateNetworkConnections()
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
                case DelayedNetworkActionType.Register:
                {
                    if (delayedActionForDestruction.pos != delayedActionForDestruction.subPart.Thing.Position)
                    {
                        break;
                    }

                    Thing parent = delayedActionForDestruction.subPart.Thing;
                    if (ComplexAt(parent.Position) != null)
                    {
                        TLog.Warning(
                            $"Tried to register trasmitter {parent} at {parent.Position}, but there is already a power net here. There can't be two transmitters on the same cell.");
                    }

                    //Ensure we only destroy network that we can also connect to
                    foreach (var subPart in delayedActionForDestruction.subPart.DirectPartSet.FullSet)
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
            Thing parentThing = delayedActionForCreation.subPart.Thing;

            switch (delayedActions[j].type)
            {
                //Create On Newly Spawned Comp
                case DelayedNetworkActionType.Register:
                {
                    TryCreateNetworkAt(delayedActionForCreation.pos, delayedActionForCreation.subPart);
                    break;
                }
                //Create By Checking Adjacent Cells Of Despawned Component
                case DelayedNetworkActionType.Deregister:
                {
                    foreach (IntVec3 adjPos in GenAdj.CellsAdjacentCardinal(delayedActionForCreation.pos,
                                 parentThing.Rotation, parentThing.def.size))
                    {
                        TryCreateNetworkAtForDestruction(adjPos, delayedActionForCreation.subPart);
                    }

                    break;
                }
            }
        }

        delayedActions.RemoveRange(0, count);
    }

    //Networks
    private void RegisterNetwork(NetworkComplex newComplex)
    {
        allComplexes.Add(newComplex);
        allNetworks.Add(newComplex.Network);
        Notify_PipeNetCreated(newComplex.Network, newComplex);
    }

    private void DeregisterNetwork(NetworkComplex oldComplex)
    {
        allComplexes.Remove(oldComplex);
        allNetworks.Remove(oldComplex.Network);
        Notify_PipeNetDestroyed(oldComplex.Network);
    }

    private void TryCreateNetworkAt(IntVec3 cell, NetworkSubPart part)
    {
        if (!cell.InBounds(map)) return;
        if (ComplexAt(cell) == null)
        {
            RegisterNetwork(PipeNetworkFactory.BuildNetwork(part, this));
        }
    }

    private void TryCreateNetworkAtForDestruction(IntVec3 cell, NetworkSubPart destroyedPart)
    {
        if (!cell.InBounds(map)) return;
        if (ComplexAt(cell) == null)
        {
            if (PipeNetworkFactory.Fits(cell.GetFirstBuilding(map), destroyedPart.NetworkDef, out var component))
            {
                RegisterNetwork(PipeNetworkFactory.BuildNetwork(component, this));
            }
        }
    }

    private void TryDestroyNetworkAt(IntVec3 cell)
    {
        if (!cell.InBounds(map)) return;
        var complex = ComplexAt(cell);
        if (complex != null)
        {
            DeregisterNetwork(complex);
        }
    }

    //Grid For Def
    public void Notify_PipeNetCreated(PipeNetwork newNetwork, NetworkComplex complex)
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
                        $"Two Pipe nets on the same cell {cell}: {lookUpGrid[num].Network.Def}[{lookUpGrid[num].Network.ID}] instead of {newNetwork.Def}[{newNetwork.ID}]");
                }
            }

            lookUpGrid[num] = complex;
            list.Add(cell);
            map.mapDrawer.MapMeshDirty(cell,
                MapMeshFlag.Buildings | MapMeshFlag.Things | MapMeshFlag.PowerGrid | MapMeshFlag.Terrain);
        }
    }

    public void Notify_PipeNetDestroyed(PipeNetwork deadNetwork)
    {
        if (!cellsByNetwork.TryGetValue(deadNetwork, out var list))
        {
            TLog.Warning(
                $"No network {deadNetwork} exists for {NetworkDef} to be destroyed! \n{nameof(cellsByNetwork)} cannot find key");
            return;
        }

        //
        for (int i = 0; i < list.Count; i++)
        {
            int num = this.map.cellIndices.CellToIndex(list[i]);
            if (lookUpGrid[num].Network == deadNetwork)
            {
                lookUpGrid[num] = null;
            }
            else if (lookUpGrid[num] != null)
            {
                TLog.Warning(
                    $"Multiple networks on the same cell {list[i]}. This is probably a result of an earlier error.");
            }
        }

        OnNetworkDestroyed();
        foreach (var networkSubPart in deadNetwork.PartSet.FullSet)
        {
            networkSubPart.Notify_NetworkDestroyed();
        }

        //
        cellsByNetwork.Remove(deadNetwork);
        allNetworks.Remove(deadNetwork);
    }

    //TODO: Add event handling pipelines

    #region EventHandling

    public event NetworkChangedEvent AddedPart;
    public event NetworkChangedEvent RemovedPart;
    public event NetworkChangedEvent NetworkDestroyed;

    #endregion

    private void OnNetworkDestroyed()
    {
        NetworkDestroyed(new NetworkChangedEventArgs(NetworkChangeType.Destroyed));
    }

    //Single Parts
    public void Notify_AddPart(INetworkSubPart part)
    {
        TotalPartSet.AddNewComponent(part);
        AddedPart(new NetworkChangedEventArgs(NetworkChangeType.AddedPart, part));
    }

    public void Notify_RemovePart(INetworkSubPart part)
    {
        TotalPartSet.RemoveComponent(part);
        RemovedPart(new NetworkChangedEventArgs(NetworkChangeType.RemovedPart, part));
    }
}
