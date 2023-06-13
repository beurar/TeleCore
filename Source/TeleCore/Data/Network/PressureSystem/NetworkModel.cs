using System.Collections.Generic;

namespace TeleCore.Network.PressureSystem;

public class FlowContent
{
    public FlowValueStack prevContent;
    public FlowValueStack content;
    public int maxContent;
}

public class NetBox
{
    private NetworkSubPart part;
    private Dictionary<NetworkSubPart, FlowContent> parts;
    
    public IList<NetBox> Neighbours { get; set; }
}

public class NetworkModel
{
    private NetworkGraph _graph;
    private List<NetBox> _netBoxes;

    public ClampWorker ClampWorker { get; set; }
    public PressureWorker PressureWorker { get; set; }
    
    public void Tick()
    {
        //1. Update Flow Content
        for(var i = 0; i < _netBoxes.Count; i++)
        {
            var box = _netBoxes[i];
            UpdateFlow(box);
        }
        
        //2. Apply Clamp Model
        for(var i = 0; i < _netBoxes.Count; i++)
        {
            var flowBox = _netBoxes[i];
            _clampModel.Apply(flowBox);
        }
        
        //3.
        for(var i = 0; i < _netBoxes.Count; i++)
        {
            var flowBox = _netBoxes[i];
            flowBox.prevContent = flowBox.content;
        }
        
        //4.
        for(var i = 0; i < _netBoxes.Count; i++)
        {
            var flowBox = _netBoxes[i];
            UpdateContent(flowBox);
        }

        //
        for(var i = 0; i < _netBoxes.Count; i++)
        {
            var fb = _netBoxes[i];
            if (fb.content < 0) UnderFlow.frame -= fb.content;
            var of = fb.content - fb.maxContent;
            if (of > 0) OverFlow.frame += of;
            UpdateFlowRate(fb);
        }
    }
    
    public void UpdateFlow(NetBox box)
    {
        foreach (var nghb in box.Neighbours)
        {
            var flow = conn.flow;
            flow = _pressureModel.GetFlow(conn, fb, flow);
            conn.flow = _clampModel.Clamp(conn, fb, flow, FLOW_SPEED);
            measureClamp = true;
            conn.move = _clampModel.Clamp(conn, fb, flow, FLUID_MOVE);
            measureClamp = false;
        }
        
        if (y > 0) {
            var pTop = fb.Conn;
            if (pTop.type != NONE) {
                f = pMid.flowTop;
                f = _pressureModel.flowFn(pTop, pMid, f);
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
}