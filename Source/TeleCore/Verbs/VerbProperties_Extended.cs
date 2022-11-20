using System.Collections.Generic;
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

        //
        public float shootHeightOffset = 0;
        public float minHitHeight = 0;
        
        //Graphical
        public MuzzleFlashProperties muzzleFlash;
        public Vector3 shotStartOffset = Vector3.zero;
        public List<Vector3> originOffsetPerShot;

        public void PostLoad()
        {
            beamProps?.SetParent(this);
        }
    }
}
