using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace TeleCore
{
    public class PortableContainer : FXThing, IFXObject, IContainerHolder
    {
        private NetworkContainer container;
        private ContainerProperties containerProps;

        public string ContainerTitle => "TC.PortableContainer".Translate();
        public Thing Thing => this;
        public NetworkContainer Container => container;
        public ContainerProperties ContainerProps => containerProps;

        public override bool[] DrawBools => new bool[1] { true };
        public override float[] OpacityFloats => new float[1] { Container?.StoredPercent ?? 0f };
        public override Color?[] ColorOverrides => new Color?[1] { Container?.Color ?? Color.white };

        public void SetContainerProps(ContainerProperties props)
        {
            this.containerProps = new ContainerProperties()
            {
                doExplosion = props.doExplosion,
                dropContents = props.leaveContainer,
                explosionRadius = props.explosionRadius,
                leaveContainer = false,
                maxStorage = props.maxStorage,
            };
        }

        public void SetContainer(NetworkContainer container)
        {
            this.container = container;
        }



        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref containerProps, "containerProps");
            Scribe_Deep.Look(ref container, "networkContainer", this);
        }

        public void Notify_ContainerFull()
        {
            //
        }
        public void Notify_ContainerStateChanged()
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

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(base.GetInspectString());
            sb.AppendLine($"{"TR_PortableContainer".Translate()}: {Container.TotalStored}/{Container.Capacity}");
            return sb.ToString().TrimEndNewlines();
        }

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
        }
    }
}
