using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    //TODO: Add leaking functionality, broken transmitters losing values
    public class Comp_NetworkStructure : ThingComp, IFXObject, INetworkStructure
    {
        //Comp References
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompFX fxComp;
        private MapComponent_TeleCore teleMapCore;

        //Fields
        private NetworkMapInfo networkInfo;
        private IntVec3[][] innerConnectionCellsByRot;
        private IntVec3[][] connectionCellsByRot;

        private List<NetworkComponent> networkParts = new();
        private readonly Dictionary<NetworkDef, NetworkComponent> networkComponentByDef = new();

        //Debug
        protected static bool DebugConnectionCells = false;

        public NetworkComponent this[NetworkDef def] => networkComponentByDef[def];

        public Thing Thing => parent;

        //CompStuff
        public CompProperties_NetworkStructure Props => (CompProperties_NetworkStructure)base.props;

        public CompPowerTrader CompPower => powerComp;
        public CompFlickable CompFlick => flickComp;
        public CompFX CompFX => fxComp;

        public bool IsPowered => CompPower?.PowerOn ?? true;

        public List<NetworkComponent> NetworkParts => networkParts;

        //
        //public NetworkStructureSet NeighbourStructureSet { get => structureSet; protected set => structureSet = value; }

        //FX Data
        public virtual bool IsMain => true;
        public virtual int Priority => 10;
        public virtual bool ShouldThrowFlecks => true;
        public virtual CompPower ForcedPowerComp => parent.GetComp<CompPowerTrader>();

        public virtual bool FX_AffectsLayerAt(int index)
        {
            return true;
        }

        public virtual bool FX_ShouldDrawAt(int index)
        {
            return index switch
            {
                1 => networkParts.Any(t => t?.HasConnection ?? false),
                _ => true,
            };
        }

        public virtual Color? FX_GetColorAt(int index)
        {
            return index switch
            {
                0 => NetworkParts[0].Container.Color,
                _ => Color.white
            };
        }

        public virtual Vector3? FX_GetDrawPositionAt(int index)
        {
            return parent.DrawPos;
        }

        public virtual float FX_GetOpacityAt(int index) => 1f;
        public virtual float? FX_GetRotationAt(int index) => null;
        public virtual float? FX_GetRotationSpeedAt(int index) => null;
        public virtual Action<FXGraphic> FX_GetActionAt(int index) => null;

        //
        public IntVec3[] InnerConnectionCells
        {
            get
            {
                return innerConnectionCellsByRot[parent.Rotation.AsInt] ??= Props.InnerConnectionCells(parent);
            }
        }

        public IntVec3[] ConnectionCells
        {
            get
            {
                if (connectionCellsByRot[parent.Rotation.AsInt] == null)
                {
                    var cellsOuter = new List<IntVec3>();
                    foreach (var edgeCell in parent.OccupiedRect().ExpandedBy(1).EdgeCells)
                    {
                        foreach (var inner in InnerConnectionCells)
                        {
                            if (edgeCell.AdjacentToCardinal(inner))
                            {
                                cellsOuter.Add(edgeCell);
                            }
                        }
                    }
                    connectionCellsByRot[parent.Rotation.AsInt] = cellsOuter.ToArray();
                }

                return connectionCellsByRot[parent.Rotation.AsInt];
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref networkParts, "networkParts", LookMode.Deep, this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (networkParts.NullOrEmpty())
                {
                    TLog.Warning($"Could not load network parts for {parent}... Correcting.");
                    return;
                }
                foreach (var newComponent in networkParts)
                {
                    networkComponentByDef.Add(newComponent.NetworkDef, newComponent);
                    newComponent.ComponentSetup(true);
                }
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            innerConnectionCellsByRot = new IntVec3[4][];
            connectionCellsByRot = new IntVec3[4][];

            //Get cached data
            powerComp = parent.TryGetComp<CompPowerTrader>();
            flickComp = parent.TryGetComp<CompFlickable>();
            fxComp = parent.TryGetComp<CompFX>();

            //
            teleMapCore =  parent.Map.TeleCore();
            networkInfo = teleMapCore.NetworkInfo;

            //Create NetworkComponents
            if (!respawningAfterLoad || networkParts.NullOrEmpty())
            {
                if (respawningAfterLoad && networkParts.NullOrEmpty())
                {
                    TLog.Warning($"Spawning {parent} after load with null parts... Correcting.");
                }
                for (var i = 0; i < Props.networks.Count; i++)
                {
                    var compProps = Props.networks[i];
                    var newComponent = (NetworkComponent)Activator.CreateInstance(compProps.workerType, args: new object[]{this, compProps, i}); //new NetworkComponent(this, compProps, i);
                    networkParts.Add(newComponent);
                    networkComponentByDef.Add(compProps.networkDef, newComponent);
                    newComponent.ComponentSetup(respawningAfterLoad);
                }
            }

            //Check for neighbor intersections


            //Regen network after all data is set
            networkInfo.Notify_NewNetworkStructureSpawned(this);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            //Regen network after all data is set
            networkInfo.Notify_NetworkStructureDespawned(this);

            foreach (var networkPart in NetworkParts)
            {
                networkPart.PostDestroy(mode, previousMap);
            }

            base.PostDestroy(mode, previousMap);
        }

        public override void CompTick()
        {
            base.CompTick();
            var isPowered = IsPowered;
            foreach (var networkPart in networkParts)
            {
                networkPart.NetworkCompTick(isPowered);
                NetworkCompProcessor(networkPart, isPowered);
            }
            NetworkTickCustom(isPowered);
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
        }

        protected virtual void NetworkTickCustom(bool isPowered)
        {

        }

        protected virtual void NetworkCompProcessor(NetworkComponent netComp, bool isPowered)
        {
        }

        public virtual bool AcceptsValue(NetworkValueDef value)
        {
            return true;
        }

        public virtual void Notify_ReceivedValue()
        {
        }

        //Data Notifiers
        public void Notify_StructureAdded(INetworkStructure other)
        {

            //structureSet.AddNewStructure(other);
        }

        public void Notify_StructureRemoved(INetworkStructure other)
        {

            //structureSet.RemoveStructure(other);
        }

        public bool ConnectsTo(INetworkStructure other)
        {
            return ConnectionCells.Any(other.InnerConnectionCells.Contains);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (DebugConnectionCells && Find.Selector.IsSelected(parent))
            {
                GenDraw.DrawFieldEdges(ConnectionCells.ToList(), Color.cyan);
                GenDraw.DrawFieldEdges(InnerConnectionCells.ToList(), Color.green);
            }

            foreach (var networkPart in NetworkParts)
            {
                networkPart.Draw();
            }
        }

        public override void PostPrintOnto(SectionLayer layer)
        {
            foreach (var networkPart in NetworkParts)
            {
                networkPart.NetworkDef.TransmitterGraphic?.Print(layer, Thing, 0);
            }
            base.PostPrintOnto(layer);
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            /*TODO: ADD THIS TO COMPONENT DESC
            if (!Network.IsWorking)
                sb.AppendLine("TR_MissingNetworkController".Translate());
            //TODO: Make reasons for multi roles
            if (!Network.ValidFor(Props.NetworkRole, out string reason))
            {
                sb.AppendLine("TR_MissingConnection".Translate() + ":");
                if (!reason.NullOrEmpty())
                {
                    sb.AppendLine("   - " + reason.Translate());
                }
            }
            */
            return sb.ToString().TrimStart().TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            /*
            foreach (var gizmo in networkParts.Select(c => c.SpecialNetworkDescription))
            {
                yield return gizmo;
            }
            */

            foreach (var networkPart in networkParts)
            {
                foreach (var partGizmo in networkPart.GetPartGizmos())
                {
                    yield return partGizmo;
                }
            }

            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (!DebugSettings.godMode) yield break;

            yield return new Command_Action()
            {
                defaultLabel = "Draw Networks",
                action = delegate
                {
                    foreach (var networkPart in networkParts)
                    {
                        networkInfo[networkPart.NetworkDef].ToggleShowNetworks();
                    }
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Draw Connections",
                action = delegate
                {
                    DebugConnectionCells = !DebugConnectionCells;
                }
            };
        }
    }
}
