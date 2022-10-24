using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class VerbProperties_Extended : VerbProperties
    {
        //Information
        public string description;

        //Functional
        public bool isProjectile = true;
        public bool avoidFriendlyFire;
        public int shotIntervalTicks = 10;
        public ThingDef secondaryProjectile;

        public BeamProperties beamProps;

        //Costs
        public float powerConsumptionPerShot = 0;
        public NetworkCost networkCostPerShot;

        //Effects
        public EffecterDef originEffecter;

        //
        public SoundDef chargeSound;

        //Graphical
        public MuzzleFlashProperties muzzleFlash;
        public List<Vector3> originOffsets;
        public Vector3 originOffset;

        public void PostLoad()
        {
            beamProps?.SetParent(this);
        }
    }
}
