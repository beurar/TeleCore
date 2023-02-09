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
    /// A basic implementation of the <see cref="IFXHolder"/> interface, uses <see cref="Pawn"/> as a base class.
    /// </summary>
    public class FXPawn : Pawn, IFXHolder
    {
        public FXDefExtension Extension => def.FXExtension();
        public CompFX FXComp => this.GetComp<CompFX>();

        public virtual bool FX_ProvidesForLayer(FXLayerArgs args) => true; //FXLayerData._ThingHolderTag;
        public virtual CompPowerTrader FX_PowerProviderFor(FXLayerArgs args) => null!;
        public virtual bool? FX_ShouldDraw(FXLayerArgs args) => null;
        public virtual float? FX_GetOpacity(FXLayerArgs args) => null;
        public virtual float? FX_GetRotation(FXLayerArgs args) => null;
        public virtual float? FX_GetRotationSpeedOverride(FXLayerArgs args) => null;
        public virtual float? FX_GetAnimationSpeedFactor(FXLayerArgs args) => null;
        public virtual Color? FX_GetColor(FXLayerArgs args) => null;
        public virtual Vector3? FX_GetDrawPosition(FXLayerArgs args) => null;
        public virtual Action<RoutedDrawArgs> FX_GetDrawAction(FXLayerArgs args) => null!;
        public virtual bool? FX_ShouldThrowEffects(FXLayerArgs args) => null;
        public virtual void FX_OnEffectSpawned(EffecterEffectSpawnedArgs effectSpawnedArgs) { }
    }
}
