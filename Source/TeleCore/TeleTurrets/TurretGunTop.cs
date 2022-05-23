using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public class TurretGunTop
    {
        private readonly TurretGun parent;
        private readonly TurretTopProperties props;
        //Barrels
        private readonly List<TurretBarrel> barrels;
        
        //
        private float rotation;
        private float targetRot = 20;
        private float rotationSpeed;
        private bool rotateClockwise = true;

        private int ticksUntilTurn;
        private int turnTicks;
        private bool targetAcquired = false;

        public TurretTopProperties Props => props;
        public List<TurretBarrel> Barrels => barrels;

        public Vector3 DrawPos => new Vector3(parent.DrawPos.x, AltitudeLayer.BuildingOnTop.AltitudeFor(), parent.DrawPos.z);

        public bool HasBarrels => props.barrels != null;

        public bool OnTarget
        {
            get
            {
                if (parent.CurrentTarget.IsValid)
                {
                    targetRot = (parent.CurrentTarget.CenterVector3 - parent.DrawPos).AngleFlat();
                    return Quaternion.Angle(rotation.ToQuat(), targetRot.ToQuat()) < 1.5f;
                }
                return false;
            }
        }

        public float CurRotation
        {
            get => rotation;
            set
            {
                if (value > 360)
                {
                    rotation = value - 360;
                }
                if (value < 0)
                {
                    rotation = value + 360;
                }
                rotation = value;
            }
        }

        public TurretGunTop(TurretGun parent, TurretTopProperties topProps)
        {
            this.parent = parent;
            props = topProps;
            if (HasBarrels)
            {
                barrels = new List<TurretBarrel>(props.barrels.Count);
                foreach (var barrel in props.barrels)
                {
                    barrels.Add(new TurretBarrel(this, barrel));
                }
            }
        }

        //
        public void Notify_TurretShot(int index)
        {
            if (HasBarrels && barrels.Count > index)
            {
                barrels[index].Notify_TurretShot();
            }
        }

        public void TurretTopTick()
        {
            //Tick Barrels
            if (HasBarrels)
            {
                barrels.ForEach(b => b.BarrelTick());
            }

            //Rotate Turret To Target Or Idle
            LocalTargetInfo currentTarget = this.parent.CurrentTarget;
            if (!currentTarget.IsValid)
            {
                if (targetAcquired)
                    targetAcquired = false;
            }
            if (currentTarget.IsValid)
            {
                targetRot = (parent.CurrentTarget.CenterVector3 - parent.DrawPos).AngleFlat();
                turnTicks = 0;
            }
            else if (ticksUntilTurn > 0)
            {
                ticksUntilTurn--;
                if (ticksUntilTurn == 0)
                {
                    rotateClockwise = !(Rand.Value > 0.5);
                    turnTicks = props.idleDuration.RandomInRange;
                }
            }
            else
            {
                targetRot += rotateClockwise ? 0.26f : -0.26f;
                turnTicks--;
                if (turnTicks <= 0)
                    ticksUntilTurn = props.idleInterval.RandomInRange;
            }
            rotation = Mathf.SmoothDampAngle(rotation, targetRot, ref rotationSpeed, 0.01f, props.aimSpeed, 0.01666f);
            if (OnTarget && !targetAcquired)
            {
                targetAcquired = true;
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(parent.ParentThing.Position, parent.ParentThing.Map, false));
            }
        }

        public void DrawTurret()
        {
            TDrawing.Draw(parent.TurretGraphic, DrawPos, Rot4.North, CurRotation, null, null);
            if (HasBarrels)
            {
                barrels.ForEach(b => b.Draw());
            }
        }
    }
}
