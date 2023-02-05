using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace TeleCore
{
    /// <summary>
    /// <para>Implementing this on a <see cref="ThingComp"/> or <see cref="Thing"/> allows you to affect the layers in a <see cref="CompFX"/> attached to the same Thing instance.                                      </para>
    /// <para>You can implement this interface on multiple parts of a Thing instance, including the base <see cref="ThingDef.thingClass"/> and the <see cref="ThingDef.comps"/>.                                        </para>
    /// <para>If multiple implementations are active, the order of priority for selecting an interface for a layer via <see cref="FX_AffectsLayerAt"/> or for <see cref="IsMain"/> is done by <see cref="Priority"/>.   </para>
    /// </summary>
 
    //Ignore this for now
    // Always First: <see cref="ThingDef.thingClass"/>. Followed by the order of <see cref="CompProperties_FX"/> set in <see cref="ThingDef.comps"/>      
    public interface IFXHolder
    {
        /// <summary>
        /// If set to true, this implementation of the interface will be used for the <see cref="ShouldDoEffects"/> and <see cref="ForcedPowerComp"/> getters.
        /// </summary>
        bool IsMain { get; }

        /// <summary>
        /// 
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Sets whether or not an attached Comp_FleckThrower should throw effects.
        /// </summary>
        bool ShouldDoEffects { get; }

        /// <summary>
        /// Allows you to set a custom power comp (which may be different than the parents')
        /// </summary>
        CompPower ForcedPowerComp { get; }

        /// <summary>
        /// Define which layers this implementation of the interface will affect.
        /// </summary>
        bool FX_AffectsLayerAt(int index);

        /// <summary>
        /// Overrides whether a layer at the same index of that value is rendered or not.
        /// </summary>
        bool FX_ShouldDrawAt(int index);

        /// <summary>
        /// Sets the opacity value of a layer at the same index as the value in the array.
        /// </summary>
        float FX_GetOpacityAt(int index);

        /// <summary>
        /// Sets the rotation value of a layer at the same index as the value in the array.
        /// </summary>
        float? FX_GetRotationAt(int index);

        /// <summary>
        /// Sets the rotation speed value of a layer at the same index as the value in the array.
        /// </summary>
        float? FX_GetRotationSpeedAt(int index);

        /// <summary>
        /// Overrides the draw color of the layer at the index of the value.
        /// </summary>
        Color? FX_GetColorAt(int index);

        /// <summary>
        /// Sets the exact render position of the layer at the index of that value.
        /// </summary>
        Vector3? FX_GetDrawPositionAt(int index);

        /// <summary>
        /// Attaches a custom function to a layer, it is run before the layer is drawn.
        /// </summary>
        Action<FXGraphic> FX_GetActionAt(int index);
    }
}
