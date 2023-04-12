#define DEBUG

using System;
using RimWorld;
using TeleCore.Data.Events;
using UnityEngine;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// A basic implementation of the <see cref="IFXLayerProvider"/> interface, uses <see cref="Building"/> as a base class.
    /// </summary>
    public class FXBuilding : Building, IFXLayerProvider, IFXEffecterProvider
    {
        public FXDefExtension Extension => def.FXExtension();
        public CompFX FXComp => this.GetComp<CompFX>();

        #region FX Implementation
        
        //Basics
        public virtual bool FX_ProvidesForLayer(FXArgs args)
        {
            if(args.layerTag == "FXBuilding")
                return true;
            return false;
        }
        
        public virtual CompPowerTrader FX_PowerProviderFor(FXArgs args) => null!;
        
        //Layer
        public virtual bool? FX_ShouldDraw(FXLayerArgs args) => null;
        public virtual float? FX_GetOpacity(FXLayerArgs args) => null;
        public virtual float? FX_GetRotation(FXLayerArgs args) => null;
        public virtual float? FX_GetRotationSpeedOverride(FXLayerArgs args) => null;
        public virtual float? FX_GetAnimationSpeedFactor(FXLayerArgs args) => null;
        public virtual int? FX_SelectedGraphicIndex(FXLayerArgs args) => null;
        public virtual Color? FX_GetColor(FXLayerArgs args) => null;
        public virtual Vector3? FX_GetDrawPosition(FXLayerArgs args) => null;
        public virtual Func<RoutedDrawArgs, bool> FX_GetDrawFunc(FXLayerArgs args) => null!;

        //Effecters
        public virtual bool? FX_ShouldThrowEffects(FXEffecterArgs args) => true;

        public virtual TargetInfo FX_Effecter_TargetAOverride(FXEffecterArgs args) => null;

        public virtual TargetInfo FX_Effecter_TargetBOverride(FXEffecterArgs args) => null;

        public virtual void FX_OnEffectSpawned(FXEffecterSpawnedEventArgs args)
        {
            
        }
        
        #endregion

        //
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
        
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
        }
    }
}
