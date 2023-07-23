using System;
using System.Collections.Generic;
using TeleCore.FlowCore;
using TeleCore.FlowCore.Events;
using TeleCore.Network.Data;
using TeleCore.Network.Flow.Clamping;
using TeleCore.Network.Flow.Pressure;
using TeleCore.Network.Graph;
using TeleCore.Primitive;

namespace TeleCore.Network.Flow;

/// <summary>
///     The main algorithm container for fluid flow.
/// </summary>
public class NetworkSystem : FlowSystem<NetworkPart, NetworkVolume, NetworkValueDef>
{
    public ClampWorker ClampWorker { get; set; }
    public PressureWorker PressureWorker { get; set; }
    
    public NetworkSystem()
    {
        ClampWorker = new ClampWorker_Overcommit();
        PressureWorker = new PressureWorker_WaveEquationDamping3();
    }

    internal void Notify_Populate(NetGraph graph)
    {
        foreach (var (node, adjacent) in graph.AdjacencyList)
        {
            var flowBox = GenerateForOrGet(node);
            var list = new List<FlowInterface<NetworkVolume, NetworkValueDef>>();
            for (var i = 0; i < adjacent.Count; i++)
            {
                var nghb = adjacent[i];
                var fb2 = GenerateForOrGet(nghb.Item2.Value);
                var conn = new FlowInterface<NetworkVolume, NetworkValueDef>(flowBox, fb2);
                list.Add(conn);
            }

            Connections.Add(flowBox, list);
        }
    }
    
    private NetworkVolume GenerateForOrGet(NetworkPart part)
    {
        if (Relations.TryGetValue(part, out var fb))
        {
            return fb;
        }
        
        fb = new NetworkVolume(part.Config.volumeConfig);
        fb.FlowEvent += OnFlowBoxEvent;
        Relations.Add(part, fb);
        return fb;
    }

    private void OnFlowBoxEvent(object sender, FlowEventArgs e)
    {
    }

    public double TotalValueFor(NetworkValueDef def)
    {
        return TotalStack[def].Value;
    }

    public double TotalValueFor(NetworkValueDef def, NetworkRole role)
    {
        //TODO: Implement
        return 0;
    }

    public void TryAddValue(NetworkVolume fb, FlowValueDef def, double amount)
    {
        throw new NotImplementedException();
    }

    public void TryRemoveValue(NetworkVolume fb, FlowValueDef def, double amount)
    {
        throw new NotImplementedException();
    }

    public bool TryConsume(NetworkVolume fb, NetworkValueDef def, double value)
    {
        return false;
    }

    public void Clear(NetworkVolume fb)
    {
        throw new NotImplementedException();
    }

    internal void Draw()
    {
    }

    internal void OnGUI()
    {
    }

    public override double FlowFunc(NetworkVolume from, NetworkVolume to, double flow)
    {
        return PressureWorker.FlowFunction(from, to, flow);
    }

    public override double ClampFunc(NetworkVolume from, NetworkVolume to, double flow, ClampType clampType)
    {
        return ClampWorker.ClampFunction(from, to, flow, clampType);
    }
}