using System;
using RimWorld;
using UnityEngine;

namespace TeleCore;

public delegate void ThingSpawnedEvent(ThingStateChangedEventArgs args);
public delegate void ThingDespawnedEvent(ThingStateChangedEventArgs args);
public delegate void ThingStateChangedEvent(ThingStateChangedEventArgs args);

//Hediffs
public delegate void PawnHediffChangedEvent(PawnHediffChangedEventArgs args);

//FX Events
/*
public delegate CompPowerTrader FXGetPowerProviderEvent(FXLayerArgs args);
public delegate bool FXGetShouldDrawEvent(FXLayerArgs args);
public delegate float FXGetOpacityEvent(FXLayerArgs args);
public delegate float? FXGetRotationEvent(FXLayerArgs args);
public delegate float? FXGetAnimationSpeedEvent(FXLayerArgs args);
public delegate Color? FXGetColorEvent(FXLayerArgs args);
public delegate Vector3? FXGetDrawPositionEvent(FXLayerArgs args);
public delegate Action<FXLayer> FXGeActionEvent(FXLayerArgs args);
public delegate bool FXShouldThrowEffectsEvent(EffecterLayerArgs args);
*/
public delegate void OnEffectSpawnedEvent(FXEffecterSpawnedEffectEventArgs spawnedEffectEventArgs);