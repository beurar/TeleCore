using System;
using System.Collections.Generic;
using TeleCore.Network.Graph;
using TeleCore.Network.PressureSystem.Clamping;
using TeleCore.Network.PressureSystem.Pressure;
using Verse;

namespace TeleCore.Network.PressureSystem;

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
        double f = 0;
        for (var i = 0; i < flowBox.Interfaces.Count; i++)
        {
            var fi = flowBox.Interfaces[i];
            if (fi.Resolved) continue;

            f = fi.Flow;
            f = PressureWorker.FlowFunction(fi.EndPoint, fi.Holder, f);
            fi.Flow = ClampWorker.ClampFunction(fi.EndPoint, fi.Holder, f, ClampType.FlowSpeed); ;
            fi.Move = ClampWorker.ClampFunction(fi.EndPoint, fi.Holder, f, ClampType.FluidMove); ;
            fi.Notify_Resolved();
            
            flowBox.Interfaces[i] = fi;
        }
    }

    private void UpdateContent(FlowBox flowBox)
    {

        for (var i = 0; i < flowBox.Interfaces.Count; i++)
        {
            var fi = flowBox.Interfaces[i];
            if(fi.Updated) continue;
            var res = fi.EndPoint.RemoveContent((float)fi.Move);
            flowBox.AddContent(res.FullDiff);
            fi.Notify_Updated();
            flowBox.Interfaces[i] = fi;
        }
    }

    private void UpdateFlowRate(FlowBox pMid)
    {
        pMid.DoAdd(out float fp, out float fn);
        pMid.FlowRate = Math.Max(fp,fn);
    }
}