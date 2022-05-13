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
    /// A basic implementation of the <see cref="IFXObject"/> interface, uses <see cref="Pawn"/> as a base class.
    /// </summary>
    public class FXPawn : Pawn, IFXObject
    {
        public FXDefExtension Extension => def.Tele().graphics;
        public CompFX FXComp => this.GetComp<CompFX>();

        public virtual bool ShouldThrowFlecks => true;
        public virtual bool[] DrawBools => new bool[1] { true };
        public virtual float[] OpacityFloats => new float[1] { 1f };
        public virtual float?[] RotationOverrides => null;
        public virtual float?[] MoveSpeeds => null;
        public virtual Color?[] ColorOverrides => null;
        public virtual Vector3?[] DrawPositions => null;
        public virtual Action<FXGraphic>[] Actions => null;
        public virtual Vector2? TextureOffset => null;
        public virtual Vector2? TextureScale => null;
        public virtual CompPower ForcedPowerComp => null;
    }
}
