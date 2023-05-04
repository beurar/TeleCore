using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TeleCore.Data.Network;

public class NetworkMapInfo : MapInformation
{
    private readonly Dictionary<NetworkDef, PipeNetworkSystem> NetworksByType = new ();
    private readonly List<PipeNetworkSystem> PipeNetworks = new ();

    public NetworkMapInfo(Map map) : base(map)
    {
    }

    public PipeNetworkSystem this[NetworkDef type] => NetworksByType.TryGetValue(type);

    public PipeNetworkSystem GetOrCreateNewNetworkSystemFor(NetworkDef networkDef)
    {
        if (NetworksByType.TryGetValue(networkDef, out var network)) return network;

        //Make New
        var networkMaster = new PipeNetworkSystem(Map, networkDef);
        NetworksByType.Add(networkDef, networkMaster);
        PipeNetworks.Add(networkMaster);
        return networkMaster;
    }

    public void Notify_NewNetworkStructureSpawned(Comp_Network structure)
    {
        foreach (var networkComponent in structure.NetworkParts)
        {
            GetOrCreateNewNetworkSystemFor(networkComponent.NetworkDef).RegisterComponent(networkComponent, structure);
        }
    }

    public void Notify_NetworkStructureDespawned(Comp_Network structure)
    {
        foreach (var networkComponent in structure.NetworkParts)
        {
            GetOrCreateNewNetworkSystemFor(networkComponent.NetworkDef).DeregisterComponent(networkComponent, structure);
        }
    }

    //Data Getters
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

    public override void Tick()
    {
        /*
        foreach (var networkSystem in PipeNetworks)
        {
            networkSystem.TickNetworks();
        }
        */
    }

    [TweakValue("Network", 0, 100)] 
    public static int NetworkTickInterval = 50;

    public override void TeleTick()
    {
        if (TFind.TickManager.CurrentMapTick % NetworkTickInterval == 0)
        {
            //TLog.Message($"Ticking all networks | {TFind.TickManager.CurrentTick}");
            foreach (var networkSystem in PipeNetworks)
            {
                networkSystem.TickNetworks();
            }
        }
    }

    public override void UpdateOnGUI()
    {
        foreach (var networkSystem in PipeNetworks)
        {
            networkSystem.DrawNetworkOnGUI();
        }
    }

    public override void Update()
    {
        foreach (var networkSystem in PipeNetworks)
        {
            networkSystem.DrawNetwork();
        }
    }
}