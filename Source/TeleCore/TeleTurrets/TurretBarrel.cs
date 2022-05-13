using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class TurretBarrel
    {
        private TurretGunTop parent;
        private TurretBarrelProperties props;
        private float currentRecoil = 0;
        private float wantedRecoil = 0;
        public float currentVelocity = 0;
        private float speed = 100;


        private static float smoothTime = 0.01f;
        private static float deltaTime = 0.01666f;

        public TurretBarrel(TurretGunTop parent, TurretBarrelProperties props)
        {
            this.parent = parent;
            this.props = props;
        }

        public void Notify_TurretShot()
        {
            wantedRecoil = 1;
            speed = parent.Props.recoilSpeed;
        }

        public void BarrelTick()
        {
            currentRecoil = Mathf.SmoothDamp(currentRecoil, wantedRecoil, ref currentVelocity, smoothTime, speed, deltaTime);
            if (wantedRecoil > 0 && ((wantedRecoil - currentRecoil) <= 0.01))
            {
                wantedRecoil = 0;
                speed = parent.Props.resetSpeed;
            }
        }

        [TweakValue("TurretGunTop_BarrelOffset", -5f, 5f)]
        private static float barrelOffset = 0f;

        public Graphic Graphic => props.graphic.Graphic;

        public Vector3 DrawPos
        {
            get
            {
                var drawPos = parent.DrawPos;
                var offset = props.barrelOffset + new Vector3(0, 0, barrelOffset) + (props.recoilOffset * currentRecoil);
                drawPos += Quaternion.Euler(0, parent.CurRotation, 0) * offset;
                drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + props.altitudeOffset;
                return drawPos;
            }
        }

        public void Draw()
        {
            TDrawing.Draw(Graphic, DrawPos, Rot4.North, parent.CurRotation, null, null);
            //Overlays.DrawMesh(mesh, DrawPos, parent.CurRotation.ToQuat(), graphic.MatSingle, 0);
        }
    }
}
