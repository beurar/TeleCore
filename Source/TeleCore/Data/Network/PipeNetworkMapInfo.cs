using System.Collections.Generic;
using TeleCore.Static;
using Verse;

namespace TeleCore.Network;

//Hierarchy
//  NetworkMapInfo
//      - NetworkManager
//          - PipeNetwork

public class PipeNetworkMapInfo : MapInformation
{
    private readonly Dictionary<NetworkDef, PipeNetworkMaster>? _managers;
    
    public PipeNetworkMaster this[NetworkDef def] => _managers.TryGetValue(def, out var value) ? value : null;
    
    public PipeNetworkMapInfo(Map map) : base(map)
    {
        //GlobalEventHandler.ThingSpawned += Notify_NewNetworkStructureSpawned;
        _managers = new Dictionary<NetworkDef, PipeNetworkMaster>();
    }

    private PipeNetworkMaster GetOrCreateNewNetworkSystemFor(NetworkDef networkDef)
    {
        if (_managers.TryGetValue(networkDef, out var network))
            return network;

        var networkMaster = new PipeNetworkMaster(Map, networkDef);
        _managers.Add(networkDef, networkMaster);
        return networkMaster;
    }

    //TODO: Currently handled by custom internal call from a comp, maybe use event subscription instead?
    public void Notify_NewNetworkStructureSpawned(Comp_Network structure)
    {
        foreach (var part in structure.NetworkParts)
            GetOrCreateNewNetworkSystemFor(part.Config.networkDef).Notify_PartSpawned(part);
    }

    public void Notify_NetworkStructureDespawned(Comp_Network structure)
    {
        foreach (var part in structure.NetworkParts)
            GetOrCreateNewNetworkSystemFor(part.Config.networkDef).Notify_PartDespawned(part);
    }

    /// <summary>
    ///     This checks whether any directly connected parts exist for Linking Graphics.
    /// </summary>
    public bool HasConnectionAtFor(Thing thing, IntVec3 c)
    {
        var networkStructure = thing.TryGetComp<Comp_Network>();
        if (networkStructure == null) return false;
        for (var i = 0; i < networkStructure.NetworkParts.Count; i++)
        {
            var networkPart = networkStructure.NetworkParts[i];
            var system = GetOrCreateNewNetworkSystemFor(networkPart.Config.networkDef);
            if (networkPart.PartIO.Connections.Any(io => io.Pos == c))
                if (system.NetworkAt(c, thing.Map) == networkPart.Network)
                    return true;
        }

        return false;
    }

    public override void TeleTick()
    {
        var tick = Find.TickManager.TicksAbs;
        var shouldTick = TFind.TickManager.CurrentMapTick % TweakValues.NetworkTickInterval == 0;
        foreach (var system in _managers)
        {
            system.Value.Tick(shouldTick, tick);
        }
    }

    public override void Update()
    {
        foreach (var system in _managers) system.Value.Draw();
    }

    public override void UpdateOnGUI()
    {
        foreach (var system in _managers) system.Value.DrawOnGUI();
    }
}