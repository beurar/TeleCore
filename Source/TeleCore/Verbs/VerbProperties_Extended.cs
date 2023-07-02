using System.Collections.Generic;
using TeleCore.Network.Bills;
using UnityEngine;
using Verse;

namespace TeleCore;

public class VerbProperties_Extended : VerbProperties
{
    public bool avoidFriendlyFire;

    public BeamProperties beamProps;

    //
    public SoundDef chargeSound;

    //Information
    public string description;

    //Functional
    public bool isProjectile = true;
    public float minHitHeight = 0;

    //Graphical
    public MuzzleFlashProperties muzzleFlash;
    public NetworkCost networkCostPerShot;

    //Effects
    public EffecterDef originEffecter;
    public List<Vector3> originOffsetPerShot;

    //Costs
    public float powerConsumptionPerShot = 0;
    public ThingDef secondaryProjectile;

    //
    public float shootHeightOffset = 0;
    public int shotIntervalTicks = 10;
    public Vector3 shotStartOffset = Vector3.zero;

    public void PostLoad()
    {
        beamProps?.SetParent(this);
    }
}