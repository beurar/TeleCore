using System.Collections.Generic;
using TeleCore.Data.Events;
using TeleCore.Data.Network.Generation;
using Verse;

namespace TeleCore.Data.Network;

public class PipeNetworkSystem
{
    internal static int MasterID = 0;

    private readonly Map map;
    private readonly PipeNetwork[] lookUpGrid;

    //
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
        lookUpGrid = new PipeNetwork[map.cellIndices.NumGridCells];

        allNetworks = new();
        cellsByNetwork = new();

        //
        TFind.TickManager.RegisterMapUITickAction(UpdateNetworkConnections);
    }

    public PipeNetwork PipeNetworkAt(IntVec3 c)
    {
        return lookUpGrid[c.Index(map)];
    }

    public bool HasNetworkConnectionAt(IntVec3 c)
    {
        return lookUpGrid[c.Index(map)] != null;
    }

    public void ToggleShowNetworks()
    {
        DEBUG_DrawNetwork = !DEBUG_DrawNetwork;
    }

    public void TickNetworks()
    {
        foreach (var network in allNetworks)
        {
            network.Tick();
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
                    if (PipeNetworkAt(parent.Position) != null)
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
    public void CreatePipeNetwork(PipeNetwork newNetwork)
    {
        allNetworks.Add(newNetwork);
        Notify_PipeNetCreated(newNetwork);
    }

    public void DeletePipeNetwork(PipeNetwork oldNetwork)
    {
        allNetworks.Remove(oldNetwork);
        Notify_PipeNetDestroyed(oldNetwork);
    }

    private void TryCreateNetworkAt(IntVec3 cell, NetworkSubPart part)
    {
        if (!cell.InBounds(map)) return;
        if (PipeNetworkAt(cell) == null)
        {
            CreatePipeNetwork(PipeNetworkMaker.RegenerateNetwork(part, this));
        }
    }

    private void TryCreateNetworkAtForDestruction(IntVec3 cell, NetworkSubPart destroyedPart)
    {
        if (!cell.InBounds(map)) return;
        if (PipeNetworkAt(cell) == null)
        {
            if (PipeNetworkMaker.Fits(cell.GetFirstBuilding(map), destroyedPart.NetworkDef, out var component))
            {
                CreatePipeNetwork(PipeNetworkMaker.RegenerateNetwork(component, this));
            }
        }
    }

    private void TryDestroyNetworkAt(IntVec3 cell)
    {
        if (!cell.InBounds(map)) return;
        PipeNetwork pipeNet = PipeNetworkAt(cell);
        if (pipeNet != null)
        {
            TLog.Message($"Destroying network at {cell}");
            DeletePipeNetwork(pipeNet);
        }
    }

    //Grid For Def
    public void Notify_PipeNetCreated(PipeNetwork newNetwork)
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
                if (lookUpGrid[num] == newNetwork)
                {
                    TLog.Warning($"Multiple identical cells in NetworkCells list of {newNetwork.Def}: {cell}");
                }
                else
                {
                    TLog.Warning(
                        $"Two Pipe nets on the same cell {cell}: {lookUpGrid[num].Def}[{lookUpGrid[num].ID}] instead of {newNetwork.Def}[{newNetwork.ID}]");
                }
            }

            lookUpGrid[num] = newNetwork;
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
            if (lookUpGrid[num] == deadNetwork)
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
