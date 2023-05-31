using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore.Network.PressureSystem;


public class FlowBox
{
    public int prevContent;
    public int content;
    public int maxContent;
}

public struct ClampModel
{
    public void Apply(FlowBox flowBox)
    {
        
    }
}

public struct PressureConfig
{
    public ConfigItem cSquared;
    public ConfigItem friction;
}

public struct ConfigItem
{
    public string desc;
    public float value;
    public FloatRange range;
}

public struct PressureModel
{
    public string modelName;
    public Func<FlowBox, FlowBox, int> flowFn;
    public Func<int> pressureFn;
    public PressureConfig config;
}

public class FrameData
{
    public int frame;
    public int total;
    public int counter;
    public string desc;
}

public class FlowSystem
{
    public List<FlowBox> flowBoxes;
    public PressureModel _pressureModel;
    public ClampModel _clampModel;

    private FrameData UnderFlow;
    private FrameData OverFlow;
    private FrameData FlowClamped;
    
    public void Update()
    {
        //1.
        for(var i = 0; i < flowBoxes.Count; i++)
        {
            var flowBox = flowBoxes[i];
            UpdateFlow(flowBox);
        }
        
        //2.
        for(var i = 0; i < flowBoxes.Count; i++)
        {
            var flowBox = flowBoxes[i];
            _clampModel.Apply(flowBox);
        }
        
        //3.
        for(var i = 0; i < flowBoxes.Count; i++)
        {
            var flowBox = flowBoxes[i];
            flowBox.prevContent = flowBox.content;
        }
        
        //4.
        for(var i = 0; i < flowBoxes.Count; i++)
        {
            var flowBox = flowBoxes[i];
            UpdateContent(flowBox);
        }

        //
        for(var i = 0; i < flowBoxes.Count; i++)
        {
            var fb = flowBoxes[i];
            if (fb.content < 0) UnderFlow.frame -= fb.content;
            var of = fb.content - fb.maxContent;
            if (of > 0) OverFlow.frame += of;
            UpdateFlowRate(fb);
        }
        
        //
        for (y = 0; y < height; y++) {
            for (x = 0; x < width; x++) {
                t = tile[x][y];
                if (t.content < 0) UnderFlow.frame -= fb.content;
                var of = t.content - t.maxContent;
                if (of > 0) overflow.frame += of;
                updateFlowRate(t);
                if (t.type != NONE) draw(t);
            }
        }
    }

    public void UpdateFlow(FlowBox fb)
    {
        var pMid = fb;
        var f;
        if (y > 0) {
            var pTop = tile[x][y-1];
            if (pTop.type != NONE) {
                f = pMid.flowTop;
                f = _pressureModel.FlowFn(pTop, pMid, f);
                pMid.flowTop = _clampModel.clampFn(pTop, pMid, f, FLOW_SPEED);
                measureClamp = true;
                pMid.moveTop = _clampModel.clampFn(pTop, pMid, f, FLUID_MOVE);
                measureClamp = false;
            } else {
                pMid.flowTop = 0;
                pMid.moveTop = 0;
            }
        }
        if (x > 0) {
            var pLeft = tile[x-1][y];
            if (pLeft.type != NONE) {
                f = pMid.flowLeft;    
                f = pressureModel.flowFn(pLeft, pMid, f); 
                pMid.flowLeft = clampModel.clampFn(pLeft, pMid, f, FLOW_SPEED);
                measureClamp = true;
                pMid.moveLeft = clampModel.clampFn(pLeft, pMid, f, FLUID_MOVE);
                measureClamp = false;
            } else {
                pMid.flowLeft = 0;
                pMid.moveLeft = 0;
            }
        }
    }
    
    public void UpdateContent(FlowBox fb)
    {
        var pMid = tile[x][y];
        if (pMid.type == NONE) return;
        if (y > 0) {
            var pTop = tile[x][y-1];
            if (pTop.type != NONE) {
                pTop.content -= pMid.moveTop;
                pMid.content += pMid.moveTop;  
            }
        }
        if (x > 0) {
            var pLeft = tile[x-1][y];
            if (pLeft.type != NONE) {
                pLeft.content -= pMid.moveLeft;
                pMid.content += pMid.moveLeft;
            }
        }
    }

    public void UpdateFlowRate(FlowBox fb)
    {
        if (t.type == NONE) {
            t.flowRate = 0;
            return;
        }
        var fp = 0;
        var fn = 0;
        var add = function(f) { if (f > 0) fp+=f; else fn-=f;};
        add(t.moveTop);
        add(t.moveLeft);
        if (t.right) add(-t.right.moveLeft);
        if (t.bottom) add(-t.bottom.moveTop);
        t.flowRate = Math.max(fp,fn);
    }
}
