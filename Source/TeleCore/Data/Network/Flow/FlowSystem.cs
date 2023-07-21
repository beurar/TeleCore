using System;
using System.Collections.Generic;
using TeleCore.FlowCore;
using TeleCore.Network.Flow.Clamping;
using TeleCore.Network.Flow.Pressure;
using TeleCore.Primitive;

namespace TeleCore.Network.Flow;

public abstract class FlowSystem<TAttach, TVolume, TValueDef> : IDisposable
where TValueDef : FlowValueDef
where TVolume : FlowVolume<TValueDef>
{
    private readonly List<TVolume> _volumes;
    private readonly List<FlowInterface<TVolume, TValueDef>> _interfaces;
    private Dictionary<TAttach, TVolume> _relations;
    private Dictionary<TVolume, List<FlowInterface<TVolume, TValueDef>>> _connections;
    
    private DefValueStack<TValueDef, double> _totalStack;

    public DefValueStack<TValueDef, double> TotalStack => _totalStack;
    public Dictionary<TAttach, TVolume> Relations => _relations;
    public Dictionary<TVolume, List<FlowInterface<TVolume, TValueDef>>> ConnectionTable => _connections;
    public double TotalValue => _totalStack.TotalValue;

    public FlowSystem()
    {
        _volumes = new List<TVolume>();
        _relations = new Dictionary<TAttach, TVolume>();
        _connections = new Dictionary<TVolume, List<FlowInterface<TVolume, TValueDef>>>();
    }
    
    public void Dispose()
    {
        _volumes.Clear();
        Relations.Clear();
        ConnectionTable.Clear();
    }

    
    public void Tick()
    {
        foreach (var _volume in _volumes)
        {
            _volume.PrevStack = _volume.Stack;
        }

        foreach (var conn in _interfaces)
        {
            UpdateFlow(conn);
        }

        foreach (var volume in _interfaces)
        {
            UpdateContent(volume);
        }
        
        foreach (var volume in _volumes)
        {
            UpdateFlowRate(volume);
        }
    }

    public abstract double FlowFunc(TVolume from, TVolume to, double flow);

    public abstract double ClampFunc(TVolume from, TVolume to, double flow, ClampType clampType);

    public void UpdateFlow(FlowInterface<TVolume, TValueDef> connection)
    {
        if (connection.PassPercent <= 0)
        {
            connection.NextFlow = 0;
            connection.Move = 0;
            return;
        }
            
        double flow = connection.NextFlow;      
        var from = connection.From;
        var to = connection.To;
        flow = FlowFunc(from, to, flow);
        connection.UpdateBasedOnFlow(flow);
        flow = Math.Abs(flow);
        connection.NextFlow = ClampFunc(from, to, flow, ClampType.FlowSpeed);
        connection.Move = ClampFunc( from, to, flow, ClampType.FluidMove);
    }
    
    private void UpdateContent(FlowInterface<TVolume, TValueDef> conn)
    {
        DefValueStack<TValueDef, double> res = conn.From.RemoveContent(conn.Move);
        conn.To.AddContent(res);
        //Console.WriteLine($"Moved: " + conn.Move + $":\n{res}");
        //TODO: Structify for: _connections[fb][i] = conn;
    }

    private void UpdateFlowRate(TVolume fb)
    {
        double fp = 0;
        double fn = 0;

        foreach (var conn in _connections[fb])
            Add(conn.Move);

        fb.FlowRate = Math.Max(fp, fn);
        return;

        void Add(double f)
        {
            if (f > 0)
                fp += f;
            else
                fn -= f;
        }
    }
}