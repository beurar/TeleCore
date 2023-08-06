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
    
    protected override NetworkVolume CreateVolume(NetworkPart part)
    {
        return new NetworkVolume(part.Config.volumeConfig);
    }

    internal void Notify_Populate(NetGraph graph)
    {
        foreach (var edge in graph.Edges)
        {
            var fb1 = GenerateForOrGet(edge.From);
            var fb2 = GenerateForOrGet(edge.To);
            var mode = edge.BiDirectional ? InterfaceFlowMode.TwoWay : InterfaceFlowMode.FromTo;
            var iFace = new FlowInterface<NetworkPart, NetworkVolume, NetworkValueDef>(edge.From, edge.To, fb1, fb2,mode);
            AddInterface((edge.From, edge.To), iFace);
        }
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

    public override double FlowFunc(FlowInterface<NetworkPart, NetworkVolume, NetworkValueDef> iface, double flow)
    {
        return PressureWorker.FlowFunction(iface, flow);
    }

    public override double ClampFunc(FlowInterface<NetworkPart, NetworkVolume, NetworkValueDef> iface, double flow, ClampType clampType)
    {
        return ClampWorker.ClampFunction(iface, flow, clampType);
    }
}