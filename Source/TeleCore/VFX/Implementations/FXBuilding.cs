using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// A basic implementation of the <see cref="IFXObject"/> interface, uses <see cref="Building"/> as a base class.
    /// </summary>
    public class FXBuilding : Building, IFXObject
    {
        public FXDefExtension Extension => def.Tele().graphics;
        public CompFX FXComp => this.GetComp<CompFX>();

        public virtual bool IsMain => true;
        public virtual int Priority => 100;
        public virtual bool ShouldThrowFlecks => true;
        public virtual CompPower ForcedPowerComp => null;
        public virtual bool FX_AffectsLayerAt(int index) => true;
        public virtual bool FX_ShouldDrawAt(int index) => true;
        public virtual float FX_GetOpacityAt(int index) => 1f;
        public virtual float? FX_GetRotationAt(int index) => null;
        public virtual float? FX_GetRotationSpeedAt(int index) => null;
        public virtual Color? FX_GetColorAt(int index) => null;
        public virtual Vector3? FX_GetDrawPositionAt(int index) => null;
        public virtual Action<FXGraphic> FX_GetActionAt(int index) => null;

        //
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }
}
