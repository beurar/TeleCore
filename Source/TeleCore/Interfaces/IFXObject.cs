using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;

namespace TeleCore
{
    public interface IFXObject
    {
        /// <summary>
        /// Sets whether or not an attached Comp_FleckThrower should throw effects.
        /// </summary>
        bool ShouldThrowFlecks { get; }

        /// <summary>
        /// Overrides whether a layer at the same index of that value is rendered or not.
        /// </summary>
        bool[] DrawBools { get; }

        /// <summary>
        /// Sets the opacity value of a layer at the same index as the value in the array.
        /// </summary>
        float[] OpacityFloats { get; }

        /// <summary>
        /// Sets the rotation value of a layer at the same index as the value in the array.
        /// </summary>
        float?[] RotationOverrides { get; }

        /// <summary>
        /// Sets the speed value of a layer at the same index as the value in the array. i.e: Rotating layers rotate at the speed of this value.
        /// </summary>
        float?[] MoveSpeeds { get; }
        
        /// <summary>
        /// Overrides the draw color of the layer at the index of the value.
        /// </summary>
        Color?[] ColorOverrides { get; }

        /// <summary>
        /// Sets the exact render position of the layer at the index of that value.
        /// </summary>
        Vector3?[] DrawPositions { get; }

        /// <summary>
        /// Attaches a custom function to a layer, it is run before the layer is drawn.
        /// </summary>
        Action<FXGraphic>[] Actions { get; }

        /// <summary>
        /// N/A yet
        /// </summary>
        Vector2? TextureOffset { get; }
        /// <summary>
        /// N/A yet
        /// </summary>
        Vector2? TextureScale { get; }

        /// <summary>
        /// Allows you to set a custom power comp (which may be different than the parent's)
        /// </summary>
        CompPower ForcedPowerComp { get; }
    }
}
