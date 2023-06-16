using System.Collections.Generic;
using TeleCore.FlowCore;
using TeleCore.Network.Graph;
using UnityEngine;
using Verse;

namespace TeleCore.Network.PressureSystem;

//TODO: Optimize by having "global" array/list for accessing containers
public class FlowBox
{
    private NetNode _holder;
    private List<FluidInterface> _interfaces;
    
    public float PrevContent { get; set; }
    public float Content => _holder.Holder.Container.TotalStored;
    public float MaxContent => _holder.Holder.Container.Capacity;

    public float FlowRate { get; set; }
    
    public List<FluidInterface> Interfaces => _interfaces;
    
    public FlowBox(NetNode node)
    {
        _holder = node;
        _interfaces = GenerateInterfaces(node);
    }

    private List<FluidInterface> GenerateInterfaces(NetNode node)
    {
        List<FluidInterface> interfaces = new List<FluidInterface>();
        foreach (var nodeInterface in node.Interfaces)
        {
            interfaces.Add(new FluidInterface(this,)); //????
        }
        return interfaces;
    }
    
    public ValueResult<NetworkValueDef> RemoveContent(float value)
    {
        var container = _holder.Holder.Container;
        var split = Mathf.RoundToInt(value / container.StoredDefs.Count);
        var result = ValueResult<NetworkValueDef>.Init(Mathf.RoundToInt(value));

        foreach (var def in container.StoredDefs)
        {
            var tmpResult = container.TryRemove(def, split);
            if (tmpResult)
            {
                var val = tmpResult.FullDiff[0];
                result.AddDiff(val.Def, val.ValueInt);
                
                if (tmpResult.ActualAmount != split)
                {
                    split = Mathf.RoundToInt((value - tmpResult.ActualAmount) / (container.StoredDefs.Count - 1));
                }
            }
        }

        return result.Resolve().Complete();
    }

    public void AddContent(DefValueStack<NetworkValueDef> stack)
    {
        _holder.Holder.Container.TryAdd(stack);
    }
}

public struct FluidInterface
{
    private bool _flowResolved;
    private double _flow;
    
    public FlowBox Holder;
    public FlowBox EndPoint;
    public NetEdge Edge;

    public bool Resolved => _flowResolved;

    internal void Notify_Dirty()
    {
        _flowResolved = false;
    }

    internal void Notify_Resolved()
    {
        _flowResolved = true;
    }
}