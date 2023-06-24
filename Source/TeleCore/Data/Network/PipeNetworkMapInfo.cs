using System.Collections.Generic;
using TeleCore.Data.Events;
using TeleCore.Defs;
using Verse;

namespace TeleCore.Network;

public class PipeNetworkMapInfo : MapInformation
{
    [TweakValue("Network", 0, 100)] 
    public static int NetworkTickInterval = 50;
    
    //
    private readonly Dictionary<NetworkDef, PipeSystem> _systemsByType;
    
    public PipeNetworkMapInfo(Map map) : base(map)
    {
        //GlobalEventHandler.ThingSpawned += Notify_NewNetworkStructureSpawned;
        _systemsByType = new Dictionary<NetworkDef, PipeSystem>();
    }
    
    private PipeSystem GetOrCreateNewNetworkSystemFor(NetworkDef networkDef)
    {
        if (_systemsByType.TryGetValue(networkDef, out var network)) 
            return network;
        
        var networkMaster = new PipeSystem(Map, networkDef);
        _systemsByType.Add(networkDef, networkMaster);
        return networkMaster;
    }
    
    //TODO: Currently handled by custom internal call from a comp, maybe use event subscription instead?
    public void Notify_NewNetworkStructureSpawned(Comp_Network structure)
    {
        foreach (var part in structure.NetworkParts)
        {
            GetOrCreateNewNetworkSystemFor(part.NetworkDef).Notify_PartSpawned(part, structure);
        }
    }

    public void Notify_NetworkStructureDespawned(Comp_Network structure)
    {
        foreach (var part in structure.NetworkParts)
        {
            GetOrCreateNewNetworkSystemFor(part.NetworkDef).Notify_PartDespawned(part, structure);
        }
    }
    
    /// <summary>
    /// This checks whether any directly connected parts exist for Linking Graphics.
    /// </summary>
    public bool HasConnectionAtFor(Thing thing, IntVec3 c)
    {
        var networkStructure = thing.TryGetComp<Comp_Network>();
        if (networkStructure == null) return false;
        foreach (var networkPart in networkStructure.NetworkParts)
        {
            if (networkPart.CellIO.VisualConnectionCells.Contains(c)) return true;
            if (networkPart.DirectPartSet[c] != null)
            {
                return true;
            }
        }
        return false;
    }

    public override void TeleTick()
    {
        if (TFind.TickManager.CurrentMapTick % NetworkTickInterval == 0)
        {
            //TLog.Message($"Ticking all networks | {TFind.TickManager.CurrentTick}");
            foreach (var system in _systemsByType)
            {
                system.TickNetworks();
            }
        }
    }
    
    public override void UpdateOnGUI()
    {
        foreach (var system in _systemsByType)
        {
            system.DrawNetworkOnGUI();
        }
    }

    public override void Update()
    {
        foreach (var system in _systemsByType)
        {
            system.DrawNetwork();
        }
    }
}