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
    ///
    /// TODO: Define "Reserved-Layers" as index or tags and allow custom layer definition without needing to skip layers, or use reference tags..
    
    public interface IFXHolder
    {
        #region MetaDataGetter

        /// <summary>
        /// 
        /// </summary>
        bool FX_ProvidesForLayer(FXLayerArgs args);
        
        /// <summary>
        /// Allows you to override the default power getter with a custom reference, otherwise it defaults to the parent Thing's PowerComp (if it exists)
        /// </summary>
        CompPowerTrader FX_PowerProviderFor(FXLayerArgs args);
        
        /*
        /// <summary>
        /// </summary>
        bool FX_Data_AffectsLayer(FXLayerArgs args);
        */
        
        #endregion
        
        /// <summary>
        /// Overrides whether a layer at the same index of that value is rendered or not.
        /// </summary>
        bool? FX_ShouldDraw(FXLayerArgs args);

        /// <summary>
        /// Sets the opacity value of a layer at the same index as the value in the array.
        /// </summary>
        float? FX_GetOpacity(FXLayerArgs args);

        /// <summary>
        /// Sets the rotation value of a layer at the same index as the value in the array.
        /// </summary>
        float? FX_GetRotation(FXLayerArgs args);

        /// <summary>
        /// Sets the speed at which the layer processes dynamic images (rotating, blinking, moving)
        /// </summary>
        float? FX_GetAnimationSpeedFactor(FXLayerArgs args);

        /// <summary>
        /// Overrides the draw color of the layer at the index of the value.
        /// </summary>
        Color? FX_GetColor(FXLayerArgs args);

        /// <summary>
        /// Sets the exact render position of the layer at the index of that value.
        /// </summary>
        Vector3? FX_GetDrawPosition(FXLayerArgs args);

        /// <summary>
        /// Attaches a custom function to a layer, it is run before the layer is drawn.
        /// </summary>
        Action<FXLayer> FX_GetAction(FXLayerArgs args);

        #region MyRegion
        
        /// <summary>
        /// Sets whether or not an attached Comp_FleckThrower should throw effects.
        /// </summary>
        bool? FX_ShouldThrowEffects(EffecterLayerArgs args);
        
        /// <summary>
        /// Allows you to hook into the effecter logic, and handle custom logic whenever a tagged effect is spawned.
        /// </summary>
        void FX_OnEffectSpawned(EffecterEffectSpawnedArgs args);

        #endregion
    }
}
