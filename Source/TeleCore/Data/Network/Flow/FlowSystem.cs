using System;
using System.Collections.Generic;
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
public class FlowSystem : IDisposable
{
    private readonly List<NetworkVolume> _flowBoxes;
    private DefValueStack<NetworkValueDef, double> _totalStack;

    public FlowSystem()
    {
        _flowBoxes = new List<NetworkVolume>();
        Relations = new Dictionary<NetworkPart, NetworkVolume>();
        ConnectionTable = new Dictionary<NetworkVolume, List<FlowInterface>>();

        ClampWorker = new ClampWorker_Overcommit();
        PressureWorker = new PressureWorker_WaveEquation();
    }

    public ClampWorker ClampWorker { get; set; }
    public PressureWorker PressureWorker { get; set; }

    internal Dictionary<NetworkPart, NetworkVolume> Relations { get; }

    internal Dictionary<NetworkVolume, List<FlowInterface>> ConnectionTable { get; }

    public double TotalValue => _totalStack.TotalValue;

    public void Dispose()
    {
        _flowBoxes.Clear();
        Relations.Clear();
        ConnectionTable.Clear();
    }

    internal void Notify_Populate(NetGraph graph)
    {
        foreach (var (node, adjacent) in graph.AdjacencyList)
        {
            var flowBox = GenerateFor(node);
            var list = new List<FlowInterface>();
            for (var i = 0; i < adjacent.Count; i++)
            {
                var nghb = adjacent[i];
                var fb2 = GenerateFor(nghb.Item2.Value);
                var conn = new FlowInterface(flowBox, fb2);
                list.Add(conn);
            }

            ConnectionTable.Add(flowBox, list);
        }
    }

    private NetworkVolume GenerateFor(NetworkPart part)
    {
        var fb = new NetworkVolume(part.Config.volumeConfig);
        fb.FlowEvent += OnFlowBoxEvent;
        Relations.Add(part, fb);
        return fb;
    }

    private void OnFlowBoxEvent(object sender, FlowEventArgs e)
    {
    }

    public double TotalValueFor(NetworkValueDef def)
    {
        return _totalStack[def].Value;
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

    #region Updates

    /// <summary>
    ///     Update flow.
    /// </summary>
    public void Tick()
    {
        foreach (var fb in _flowBoxes)
        {
            fb.PrevStack = fb.Stack;
            ConnectionTable[fb].ForEach(c => c.Notify_SetDirty());
        }

        foreach (var flowBox in _flowBoxes) UpdateFlow(flowBox);

        foreach (var flowBox in _flowBoxes) UpdateContent(flowBox);

        foreach (var flowBox in _flowBoxes) UpdateFlowRate(flowBox);
    }

    private void UpdateFlow(NetworkVolume fb)
    {
        for (var i = 0; i < ConnectionTable[fb].Count; i++)
        {
            var conn = ConnectionTable[fb][i];
            if (conn.ResolvedFlow) continue;

            var flow = conn.Flow;
            flow = PressureWorker.FlowFunction(conn.To, conn.From, flow);
            conn.Flow = ClampWorker.ClampFunction(conn.To, conn.From, flow, ClampType.FlowSpeed);
            conn.Move = ClampWorker.ClampFunction(conn.To, conn.From, flow, ClampType.FluidMove);
            conn.Notify_ResolvedFlow();

            //TODO: Structify for: _connections[fb][i] = conn;
        }
    }

    private void UpdateContent(NetworkVolume fb)
    {
        for (var i = 0; i < ConnectionTable[fb].Count; i++)
        {
            var conn = ConnectionTable[fb][i];
            if (conn.ResolvedMove) continue;
            var res = conn.To.RemoveContent(conn.Move);
            fb.AddContent(res);
            conn.Notify_ResolvedMove();

            //TODO: Structify for: _connections[fb][i] = conn;
        }
    }

    private void UpdateFlowRate(NetworkVolume fb)
    {
        double fp = 0;
        double fn = 0;

        void Add(double f)
        {
            if (f > 0)
                fp += f;
            else
                fn -= f;
        }

        foreach (var conn in ConnectionTable[fb]) Add(conn.Move);

        //
        fb.FlowRate = Math.Max(fp, fn);
    }

    #endregion
}