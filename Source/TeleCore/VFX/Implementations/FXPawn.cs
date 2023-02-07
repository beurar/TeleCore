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

        public CompPowerTrader FX_PowerProviderFor(FXLayerArgs args) => null!;
        public bool FX_ShouldDraw(FXLayerArgs args) => true;
        public float FX_GetOpacity(FXLayerArgs args) => 1f;
        public float? FX_GetRotation(FXLayerArgs args) => null;
        public float? FX_GetAnimationSpeedFactor(FXLayerArgs args) => null;
        public Color? FX_GetColor(FXLayerArgs args) => null;
        public Vector3? FX_GetDrawPosition(FXLayerArgs args) => null;
        public Action<FXLayer> FX_GetAction(FXLayerArgs args) => null!;
        public bool FX_ShouldThrowEffects(string effecterTag) => true;
        public void FX_OnEffectSpawned(FXEffecterArgs args) { }
    }
}
