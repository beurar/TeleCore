using System;
using System.Collections.Generic;
using TeleCore.Network.Data;
using TeleCore.Network.Flow.Clamping;
using TeleCore.Network.Flow.Pressure;
using TeleCore.Network.Graph;
using Enumerable = System.Linq.Enumerable;

namespace TeleCore.Network.Flow;

/// <summary>
/// The main algorithm container for fluid flow.
/// </summary>
public class FlowSystem
{
    private List<FlowBox> _flowBoxes;
    private Dictionary<NetworkPart, FlowBox> _flowBoxByPart;
    private Dictionary<FlowBox, List<FlowInterface>> _connections;

    public ClampWorker ClampWorker { get; set; }
    public PressureWorker PressureWorker { get; set; }

    internal Dictionary<NetworkPart, FlowBox> Relations => _flowBoxByPart;
    internal Dictionary<FlowBox, List<FlowInterface>> ConnectionTable => _connections;
    
    public FlowSystem()
    {
        _flowBoxes = new List<FlowBox>();
        _flowBoxByPart = new Dictionary<NetworkPart, FlowBox>();
        _connections = new Dictionary<FlowBox, List<FlowInterface>>();
        
        ClampWorker = new ClampWorker_Overcommit();
        PressureWorker = new PressureWorker_WaveEquation();
    }

    internal void Notify_Populate(NetGraph graph)
    {
        foreach (var (node, adjacent) in graph.AdjacencyList)
        {
            var flowBox = GenerateFor(node);
            var list = new List<FlowInterface>();
            for (var i = 0; i < adjacent.Count; i++)
            {
                (NetEdge, NetNode) nghb = adjacent[i];
                var fb2 = GenerateFor(nghb.Item2.Value);
                var conn = new FlowInterface(flowBox, fb2);
                list.Add(conn);
            }

            _connections.Add(flowBox, list);
        }
    }

    private FlowBox GenerateFor(NetworkPart part)
    {
        var fb = new FlowBox(1,1,0);
        _flowBoxByPart.Add(part, fb);
        return fb;
    }

    /// <summary>
    /// Update flow.
    /// </summary>
    public void Tick()
    {
        foreach (var fb in _flowBoxes)
        {
            fb.PrevStack = fb.Stack;
            _connections[fb].ForEach(c => c.Notify_SetDirty());
        }
        
        foreach (var flowBox in _flowBoxes)
        {
            UpdateFlow(flowBox);
        }

        foreach (var flowBox in _flowBoxes)
        {
            UpdateContent(flowBox);
        }

        foreach (var flowBox in _flowBoxes)
        {
            UpdateFlowRate(flowBox);
        }
    }

    private void UpdateFlow(FlowBox fb)
    {
        double f = 0;
        for (var i = 0; i < _connections[fb].Count; i++)
        {
            var conn = _connections[fb][i];
            if (conn.ResolvedFlow) continue;

            f = conn.Flow;
            f = PressureWorker.FlowFunction(conn.To, conn.From, f);
            conn.Flow = ClampWorker.ClampFunction(conn.To, conn.From, f, ClampType.FlowSpeed); ;
            conn.Move = ClampWorker.ClampFunction(conn.To, conn.From, f, ClampType.FluidMove); ;
            conn.Notify_ResolveFlow();
            
            //TODO: Structify for: _connections[fb][i] = conn;
        }
    }

    private void UpdateContent(FlowBox fb)
    {
        for (var i = 0; i < _connections[fb].Count; i++)
        {
            var conn = _connections[fb][i];
            if(conn.ResolvedMove) continue;
            var res = conn.To.RemoveContent(conn.Move);
            fb.AddContent(res);
            conn.Notify_ResolveMove();
            
            //TODO: Structify for: _connections[fb][i] = conn;
        }
    }
    
    private void UpdateFlowRate(FlowBox fb)
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
        
        foreach (var conn in _connections[fb])
        {
            Add(conn.Move);
        }

        //
        fb.FlowRate = Math.Max(fp, fn);
    }

    internal void Draw()
    {
        
    }

    internal void OnGUI()
    {
        
    }
}