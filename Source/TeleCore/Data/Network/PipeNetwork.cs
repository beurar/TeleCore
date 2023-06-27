using System;
using System.Collections.Generic;
using TeleCore.Defs;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using TeleCore.Network.Utility;
using Verse;
using DebugTools = TeleCore.Static.Utilities.DebugTools;

namespace TeleCore.Network;

public class PipeNetwork : IDisposable
{
    private NetworkDef _def;
    private int _id;

    private NetGraph _graph;
    private FlowSystem _flowSystem;
    protected NetworkPartSetExtended _partSet;    
    
    //Debug
    private bool DEBUG_DrawGraph;
    private bool DEBUG_DrawFlowPressure;
    
    public int ID => _id;
    public NetworkDef NetworkDef => _def;
    
    public NetGraph Graph => _graph;
    public FlowSystem FlowSystem => _flowSystem;
    public NetworkPartSetExtended PartSet => _partSet;
    
    public bool IsWorking => !_def.UsesController || (PartSet.Controller?.IsWorking ?? false);


    public PipeNetwork(NetworkDef def)
    {
        _id = PipeNetworkFactory.MasterNetworkID++;
        _def = def;
        _partSet = new NetworkPartSetExtended(def);
    }

    public void Dispose()
    {
        _graph.Dispose();
        _flowSystem.Dispose();
        _partSet.Dispose();
    }
    
    internal void PrepareForRegen(out NetGraph graph, out FlowSystem flow)
    {
        graph = _graph = new NetGraph();
        flow = _flowSystem = new FlowSystem();
    }

    internal bool Notify_AddPart(INetworkPart part)
    {
        return _partSet.AddComponent(part);
    }

    //TODO: not needed right now as networks are always fully re-generated
    /*
    internal void Notify_RemovePart(INetworkPart part)
    {
        ParentSystem.Notify_RemovePart(part);

        //
        PartSet.RemoveComponent(part);
        containerSet.RemoveContainerFrom(part);
        foreach (var cell in part.Parent.Thing.OccupiedRect())
        {
            NetworkCells.Remove(cell);
        }
    }
    */
    
    internal void Tick()
    {
        _flowSystem.Tick();
        foreach (var part in _partSet.TickSet)
        {
            part.Tick();
        }
    }

    internal void Draw()
    {
        _flowSystem.Draw();
        _graph.Draw();

        if (DEBUG_DrawFlowPressure)
        {
            DebugTools.Debug_DrawPressure(_flowSystem);
        }
    }

    internal void OnGUI()
    {
        _flowSystem.OnGUI();
        _graph.OnGUI();
        
        if (DEBUG_DrawGraph)
        {
            DebugTools.Debug_DrawGraphOnUI(_graph);
        }
    }

    internal IEnumerable<Gizmo> GetGizmos()
    {
        if (DebugSettings.godMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "Draw Graph",
                defaultDesc = "Renders the graph which represents connections between structures.",
                icon = BaseContent.WhiteTex,
                action = delegate
                {
                    DEBUG_DrawGraph = !DEBUG_DrawGraph;
                }
            };
        }
        yield break;
    }
}