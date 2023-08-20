using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TeleCore.Network.Flow;
using TeleCore.Network.IO;
using TeleCore.Network.Utility;
using UnityEngine;
using Verse;

namespace TeleCore.Network.Data;

[DebuggerDisplay("{Thing}")]
public class NetworkPart : INetworkPart, IExposable
{
    private NetworkPartConfig _config;
    private INetworkStructure _parent;
    private PipeNetwork _network;
    private NetworkIO _networkIO;
    private NetworkIOPartSet _adjacentSet;
    private float _passThrough = 1; //Must be initalized with 100%
    private bool _isReady;

    public NetworkPartConfig Config
    {
        get => _config;
        set => _config = value;
    }

    public INetworkStructure Parent
    {
        get => _parent;
        set => _parent = value;
    }

    public Thing Thing => Parent.Thing;

    internal PipeNetwork Network
    {
        get => _network;
        set
        {
            _isReady = value != null;
            _network = value;
        }
    }

    PipeNetwork INetworkPart.Network
    {
        get => Network;
        set => Network = value;
    }

    public NetworkIO PartIO
    {
        get => _networkIO ?? Parent.GeneralIO;
    }

    public NetworkIOPartSet AdjacentSet => _adjacentSet;

    public NetworkVolume Volume => ((Network?.NetworkSystem?.Relations?.TryGetValue(this, out var vol) ?? false) ? vol : null)!;

    public bool CanBeNode => _config.volumeConfig != null;
    
    public bool IsController => (Config.roles | NetworkRole.Controller) == NetworkRole.Controller;

    public bool IsEdge => Config.roles == NetworkRole.Transmitter;
    public bool IsNode => (!IsEdge || IsJunction) && CanBeNode;
    public bool IsJunction => Config.roles == NetworkRole.Transmitter && _adjacentSet[NetworkRole.Transmitter]?.Count > 2;
    public bool HasConnection => _adjacentSet[NetworkRole.Transmitter]?.Count > 0;
    
    public bool IsReady => _isReady;
    public bool IsWorking => true;
    public bool IsReceiving { get; }
    public bool HasContainer => Volume != null;
    public bool IsLeaking { get; }
    public float PassThrough => _passThrough;

    #region Constructors

    public NetworkPart()
    {
    }

    public NetworkPart(INetworkStructure parent)
    {
        Parent = parent;
    }
    
    //Main creation in Comp_Network with Activator.
    public NetworkPart(INetworkStructure parent, NetworkPartConfig config) : this(parent)
    {
        Config = config;
        _adjacentSet = new NetworkIOPartSet(config.networkDef);
        if (config.netIOConfig != null)
            _networkIO = new NetworkIO(config.netIOConfig, parent.Thing.Position, parent.Thing.Rotation);
    }
    
    #endregion
    
    public void ExposeData()
    {
        Scribe_Values.Look(ref _passThrough, "passThrough");
    }
    
    public void PartSetup(bool respawningAfterLoad)
    {
        GetDirectlyAdjacentNetworkParts();
    }
    
    public void PostDestroy(DestroyMode mode, Map map)
    {
        //Notify adjacent parts
        foreach (var adjPart in _adjacentSet)
        {
            adjPart.AdjacentSet.RemoveComponent(this);
        }
    }

    public void Tick()
    {
    }

    #region Data

    public void SetPassThrough(float f)
    {
        _passThrough = f;
    }

    #endregion

    #region Helpers
    
    private void GetDirectlyAdjacentNetworkParts()
    {
        for (var c = 0; c < PartIO.Connections.Count; c++)
        {
            IntVec3 connectionCell = PartIO.Connections[c];
            List<Thing> thingList = connectionCell.GetThingList(Thing.Map);
            for (var i = 0; i < thingList.Count; i++)
            {
                var thing = thingList[i];
                if (thing is not ThingWithComps twc) continue;
                if (PipeNetworkFactory.Fits(twc, _config.networkDef, out var subPart))
                {
                    var result = HasIOConnectionTo(subPart);
                    var resultReverse = subPart.HasIOConnectionTo(this);
                    if (result && resultReverse)
                    {
                        _adjacentSet.AddComponent(subPart, result);
                        subPart.AdjacentSet.AddComponent(this, resultReverse);
                    }
                }
            }
        }
    }

    #endregion

    public IOConnectionResult HasIOConnectionTo(INetworkPart other)
    {
        if (other == this) return IOConnectionResult.Invalid;
        if (!Config.networkDef.Equals(other.Config.networkDef)) 
            return IOConnectionResult.Invalid;
        if (!Parent.CanConnectToOther(other.Parent)) 
            return IOConnectionResult.Invalid;
        return PartIO.ConnectsTo(other.PartIO);
    }

    public string InspectString()
    {
        StringBuilder sb = new StringBuilder();
        if (DebugSettings.godMode)
        {
            sb.AppendLine($"IsController: {IsController}");
            sb.AppendLine($"IsNode: {IsNode}");
            sb.AppendLine($"IsEdge: {IsEdge}");
            sb.AppendLine($"IsJunction: {IsJunction}");
            sb.AppendLine($"IsReady: {IsReady}");
            sb.AppendLine($"IsWorking: {IsWorking}");
            sb.AppendLine($"HasContainer: {HasContainer}");
            sb.AppendLine($"HasConnection: {HasConnection}");
            sb.AppendLine($"IsLeaking: {IsLeaking}");
            sb.AppendLine($"PassThrough: {PassThrough}");
        }

        return sb.ToString();
    }

    public virtual IEnumerable<Gizmo> GetPartGizmos()
    {
        if (DebugSettings.godMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "Adjancy Set",
                defaultDesc = _adjacentSet.ToString(),
                action = {}
            };
            /*if (IsController)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "Show Entire Network",
                    action = delegate
                    {
                        DebugNetworkCells = !DebugNetworkCells;
                    }
                };
            }

            if (Network == null) yield break;

            yield return new Command_Action
            {
                defaultLabel = $"Draw Graph",
                action = delegate { Network.DrawInternalGraph = !Network.DrawInternalGraph; }
            };

            yield return new Command_Action
            {
                defaultLabel = $"Draw AdjacencyList",
                action = delegate { Network.DrawAdjacencyList = !Network.DrawAdjacencyList; }
            };


            yield return new Command_Action
            {
                defaultLabel = $"Draw FlowDirections",
                action = delegate { Debug_DrawFlowDir = !Debug_DrawFlowDir; }
            };*/
        }

        if(Network != null)
        {
            foreach (var g in Network.GetGizmos()) 
                yield return g;
        }
    }

    #region Rendering

    public void Draw()
    {
        if (Volume == null) return;
        GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
        r.center = Parent.Thing.Position.ToVector3() + new Vector3(0.075f, AltitudeLayer.MetaOverlays.AltitudeFor(), 0.75f);
        r.size = new Vector2(1.5f, 0.15f);
        r.fillPercent = (float)(Volume?.FillPercent ?? 0f);
        r.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.green);
        r.unfilledMat =  SolidColorMaterials.SimpleSolidColorMaterial(Color.grey);
        r.margin = 0f;
        r.rotation = Rot4.East;
        GenDraw.DrawFillableBar(r);

    }

    public void Print(SectionLayer layer)
    {
        Config.networkDef.TransmitterGraphic?.Print(layer, Thing, 0, this);
    }

    #endregion
}