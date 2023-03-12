using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.FlowCore;
using TeleCore.Static;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// Temporary <see cref="NetworkContainer"/> Thing spawned upon deconstruction of a <see cref="Building"/> containing a <see cref="Comp_NetworkStructure"/> comp.
    /// </summary>
    public class PortableNetworkContainer : FXThing, IContainerImplementer<NetworkValueDef, IContainerHolderNetworkThing, NetworkContainerThing<IContainerHolderNetworkThing>>, IContainerHolderNetworkThing
    {
        //Portable Data
        private NetworkDef networkDef;
        
        //Targeting
        private TargetingParameters? paramsInt;
        private LocalTargetInfo currentDesignatedTarget = LocalTargetInfo.Invalid;
        
        //
        public NetworkDef NetworkDef => networkDef;
        public NetworkContainerThing<IContainerHolderNetworkThing> Container { get; private set; }
        
        //JobStats
        
        public float EmptyPercent => Container.StoredPercent - 1f;
        
        //
        public string ContainerTitle => "TELE.PortableContainer.Title".Translate();

        public Thing Thing => this;
        public bool ShowStorageGizmo => true;
        
        public LocalTargetInfo TargetToEmptyAt => currentDesignatedTarget;
        public bool HasValidTarget => currentDesignatedTarget.IsValid;
        
        public void Notify_ContainerStateChanged(NotifyContainerChangedArgs<NetworkValueDef> args)
        {
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                Vector3 v = GenMapUI.LabelDrawPosFor(Position);
                GenMapUI.DrawThingLabel(v, Container.StoredPercent.ToStringPercent(), Color.white);
            }
        }

        public override void Draw()
        {
            base.Draw();
            if (HasValidTarget && Find.Selector.IsSelected(this))
            {
                GenDraw.DrawLineBetween(DrawPos, currentDesignatedTarget.CenterVector3, SimpleColor.White);
            }
        }
        
        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(base.GetInspectString());
            return sb.ToString().TrimEndNewlines();
        }
        
        /// <summary>
        /// TargetParams for structures to fill this into
        /// </summary>
        private TargetingParameters ForSelf => paramsInt ??= new TargetingParameters
        {
            canTargetBuildings = true,
            validator = (info) =>
            {
                if (info.Thing is ThingWithComps twc)
                {
                    var n = twc.TryGetComp<Comp_NetworkStructure>();
                    if (n == null) return false;
                    if (!n[networkDef].NetworkRole.HasFlag(NetworkRole.Storage)) return false;
                    if (!Container.StoredDefs.Any(n.AcceptsValue)) return false;
                    if (n[networkDef].Container.FillState == ContainerFillState.Full) return false;
                    return true;
                }
                return false;
            },
        };

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            foreach (Gizmo g in Container.GetGizmos())
            {
                yield return g;
            }

            yield return new Command_Target
            {
                defaultLabel = "TELE.PortableContainer.Designator".Translate(),
                defaultDesc = "TELE.PortableContainer.DesignatorDesc".Translate(),
                icon = BaseContent.BadTex,
                targetingParams = ForSelf,
                action = (info) =>
                {
                    currentDesignatedTarget = info;
                }
            };
        }
        
        //Job-Hook
        public void Notify_FinishEmptyingToTarget()
        {
            if (Container.FillState == ContainerFillState.Empty)
            {
                DeSpawn();
                return;
            }
            currentDesignatedTarget = LocalTargetInfo.Invalid;
        }

        //
        public static PortableNetworkContainer Create(NetworkContainer container)
        {
            var containerDef = container.Config.droppedContainerDef ?? container.Holder.NetworkDef.portableContainerDefFallback;
            var portable = (PortableNetworkContainer)ThingMaker.MakeThing(containerDef);
            portable.Container = container.Copy<NetworkContainerThing<IContainerHolderNetworkThing>>();
            return portable;
        }

        public static PortableNetworkContainer CreateFromStack(ThingDef portableThingDef, DefValueStack<NetworkValueDef> stack)
        {
            var portable = (PortableNetworkContainer)ThingMaker.MakeThing(portableThingDef);
            portable.Container = new NetworkContainerThing<IContainerHolderNetworkThing>(new ContainerConfig
            {
                containerClass = null,
                baseCapacity = Mathf.RoundToInt(stack.TotalValue),
                containerLabel = null,
                storeEvenly = true,
                dropContents = false,
                leaveContainer = false,
                valueDefs = new List<FlowValueDef>(stack.Defs),
            }, portable);
            portable.Container.LoadFromStack(stack);
            return portable;
        }
    }
    
    /*
    public class PortableContainerThing2 : FXThing, IContainerHolderNetworkThing
    {
        private NetworkDef networkDef;
        private NetworkContainerThing container;
        private ContainerProperties containerProps;

        //Target Request
        private TargetingParameters paramsInt;
        private LocalTargetInfo currentDesignatedTarget = LocalTargetInfo.Invalid;

        //Container Holder
        public string ContainerTitle => "TELE.PortableContainer.Title".Translate();
        public ContainerProperties ContainerProps => containerProps;
        public NetworkContainerThing Container => container;
        public Thing Thing => this;
        public bool ShowStorageForThingGizmo => true;

        public NetworkDef NetworkDef => networkDef;
        public float EmptyPercent => Container.StoredPercent - 1f;

        //Target Request
        public LocalTargetInfo TargetToEmptyAt => currentDesignatedTarget;
        public bool HasValidTarget => currentDesignatedTarget.IsValid;

        //FX
        public override bool FX_ProvidesForLayer(FXArgs args) => true;

        public override bool? FX_ShouldDraw(FXLayerArgs args) => true;

        public override float? FX_GetOpacity(FXLayerArgs args) => Container.StoredPercent;

        public override Color? FX_GetColor(FXLayerArgs args)
        {
            return args.layerTag switch
            {
                "Reserved.Network.ContainerFill" => Container?.Color ?? Color.white
            };
        }

        public override Vector3? FX_GetDrawPosition(FXLayerArgs args)
        {
            return DrawPos + Vector3.one;
        }

        //
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref containerProps, "containerProps");
            Scribe_Deep.Look(ref container, "networkContainer", this);
        }

        //Setup
        public void SetupProperties(NetworkDef previousNetwork, NetworkContainer container, ContainerProperties props)
        {
            this.networkDef = previousNetwork;
            this.container = container.Copy<NetworkContainerThing, IContainerHolderNetworkThing>(this);
            this.containerProps = props.Copy();
            this.containerProps.leaveContainer = false;
        }
        
        public void SetupProperties(NetworkDef networkDef, DefValueStack<NetworkValueDef> valueStack, ContainerProperties props)
        {
            this.networkDef = networkDef;
            this.container = new NetworkContainerThing(this, valueStack);
            this.containerProps = props.Copy();
            this.containerProps.leaveContainer = false;

        }

        //
        public virtual void Notify_ContainerFull() { }
        public virtual void Notify_ContainerStateChanged() { }
        public void Notify_AddedContainerValue(NetworkValueDef def, float value)
        {
        }

        //Job-Hook
        public void Notify_FinishEmptyingToTarget()
        {
            if (Container.Empty)
            {
                DeSpawn();
                return;
            }
            currentDesignatedTarget = LocalTargetInfo.Invalid;
        }

        //
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            {
                Vector3 v = GenMapUI.LabelDrawPosFor(Position);
                GenMapUI.DrawThingLabel(v, Container.StoredPercent.ToStringPercent(), Color.white);
            }
        }

        public override void Draw()
        {
            base.Draw();
            if (HasValidTarget && Find.Selector.IsSelected(this))
            {
                GenDraw.DrawLineBetween(DrawPos, currentDesignatedTarget.CenterVector3, SimpleColor.White);
            }
        }

        //
        private TargetingParameters ForSelf => paramsInt ??= new TargetingParameters
        {
            canTargetBuildings = true,
            validator = (info) =>
            {
                if (info.Thing is ThingWithComps twc)
                {
                    var n = twc.TryGetComp<Comp_NetworkStructure>();
                    if (n == null) return false;
                    if (!n[NetworkDef].NetworkRole.HasFlag(NetworkRole.Storage)) return false;
                    if (!Container.AllStoredTypes.Any(n.AcceptsValue)) return false;
                    if (n[networkDef].Container.Full) return false;
                    return true;
                }
                return false;
            },
        };

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            foreach (Gizmo g in Container.GetGizmos())
            {
                yield return g;
            }

            yield return new Command_Target
            {
                defaultLabel = "TELE.PortableContainer.Designator".Translate(),
                defaultDesc = "TELE.PortableContainer.DesignatorDesc".Translate(),
                icon = BaseContent.BadTex,
                targetingParams = ForSelf,
                action = (info) =>
                {
                    currentDesignatedTarget = info;
                }
            };
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(base.GetInspectString());
            return sb.ToString().TrimEndNewlines();
        }
    }
    */
}
