using System;
using System.Collections.Generic;
using TeleCore.Network.Graph;
using TeleCore.Network.PressureSystem.Clamping;
using TeleCore.Network.PressureSystem.Pressure;
using Verse;

namespace TeleCore.Network.PressureSystem;

public class NetworkComplex
{
    private PipeNetwork network;
    private NetGraph graph;
    private FlowSystem flow;

    public PipeNetwork Network => network;
    public NetGraph Graph => graph;
    public FlowSystem FlowSystem => flow;
    
    public static NetworkComplex Create(INetworkSubPart root, PipeNetworkSystem system, out PipeNetwork newNet,
        out NetGraph newGraph, out FlowSystem newFlowSys)
    {
        var complex = new NetworkComplex();
        newNet = new PipeNetwork(root.NetworkDef, system);
        newGraph = new NetGraph();
        newFlowSys = new FlowSystem();
        
        //
        newNet.Graph = newGraph;

        return complex;
    }

    public void Tick()
    {
        network.Tick();
        flow.Tick();
    }
}

public class FlowSystem
{
    public List<FlowBox> _flowBoxes;

    public ClampWorker ClampWorker { get; set; }
    public PressureWorker PressureWorker { get; set; }

    public FlowSystem()
    {
        
    }

    #region Helpers

    public void RegisterFB(NetNode part)
    {
        var newFB = new FlowBox(part);
        _flowBoxes.Add(newFB);
    }

    #endregion

    public void Tick()
    {
        foreach (var flowBox in _flowBoxes)
        {
            flowBox.PrevContent = flowBox.Content;
            flowBox.Notify_SetDirty();
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

    private void UpdateFlow(FlowBox flowBox)
    {
        float f = 0;
        foreach (var fi in flowBox.Interfaces)
        {
            if(fi.Resolved) continue;

            f = fi.Flow;
            f = PressureWorker.FlowFunction(fi.EndPoint, fi.Holder, f);
            fi.Flow = ClampWorker.ClampFunction(fi.EndPoint, fi.Holder, f, ClampType.FlowSpeed);;
            fi.Move = ClampWorker.ClampFunction(fi.EndPoint, fi.Holder, f, ClampType.FluidMove);;
            fi.Notify_Resolved();
        }
        
        var pMid = flowBox;
        var pTop = flowBox.Top;
        
        var f = pMid.FlowTop;
        f = PressureWorker.FlowFunction(pTop, pMid, f);
        pMid.FlowTop = ClampWorker.ClampFunction(pTop, pMid, f, ClampType.FlowSpeed);
        pMid.MoveTop = ClampWorker.ClampFunction(pTop, pMid, f, ClampType.FluidMove);

        var pLeft = flowBox.Left;
        f = pMid.FlowLeft;
        f = PressureWorker.FlowFunction(pLeft, pMid, f);
        pMid.FlowLeft = ClampWorker.ClampFunction(pLeft, pMid, f, ClampType.FlowSpeed);
        pMid.MoveLeft = ClampWorker.ClampFunction(pLeft, pMid, f, ClampType.FluidMove);
    }

    private void UpdateContent(FlowBox flowBox)
    {
        var pMid = flowBox;
        
        var pTop = flowBox.Top;
        var res = pTop.RemoveContent(pMid.MoveTop);
        pMid.AddContent(res.FullDiff);

        var pLeft = flowBox.Left;
        var res2 = pLeft.RemoveContent(pMid.MoveLeft);
        pMid.AddContent(res2.FullDiff);
    }

    private void UpdateFlowRate(FlowBox pMid)
    {
        pMid.DoAdd(out float fp, out float fn);
        pMid.FlowRate = Math.Max(fp,fn);
    }
}