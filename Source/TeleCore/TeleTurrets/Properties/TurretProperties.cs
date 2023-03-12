using System;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class TurretProperties
    {
        public Type turretGunClass = typeof(TurretGun);
        public string label = "Turret Gun";
        public TurretTopProperties turretTop;
        public ThingDef turretGunDef;
        public Vector3 turretOffset;

        public float turretBurstWarmupTime;
        public float turretBurstCooldownTime = -1f;
        public float turretInitialCooldownTime;

        public bool continuous = false;
        public bool canForceTarget = false;

        //public TurretBurstMode burstMode = TurretBurstMode.Normal;
    }
}
