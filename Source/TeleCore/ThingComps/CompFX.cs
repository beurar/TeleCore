using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public struct FXParentInfo
    {
        public int TickOffset { get; }
        public int SpawnTick { get; }
        public FXDefExtension Extension { get; }
        public Thing ParentThing { get; }
        
        //
        public ThingDef Def => ParentThing.def;
        
        public FXParentInfo(int tickOffset, int spawnTick, FXDefExtension extension, Thing parentThing)
        {
            TickOffset = tickOffset;
            SpawnTick = spawnTick;
            Extension = extension;
            ParentThing = parentThing;
        }
    }
    
    public class CompFX : ThingComp
    {
        //
        private bool spawnedOnce = false;
        private List<IFXHolder> allHeldFXComps;

        //TODO: Performance comparision
        //private IFXHolder[] FXByLayerIndex;
        
        //Events
        private FXGetPowerProviderEvent GetPowerProvider;
        private FXGetShouldDrawEvent GetShouldDraw;
        private FXGetRotationEvent GetRotation;
        private FXGetAnimationSpeedEvent GetAnimationSpeed;
        private FXGetDrawPositionEvent GetDrawPosition;
        private FXGetColorEvent GetColor;
        private FXGetOpacityEvent GetOpacity;
        private FXGeActionEvent GetAction;

        private FXShouldThrowEffectsEvent GetShouldThrowEffects;
        private FXOnEffectSpawnedEvent EffectSpawned;
        
        //Debug
        internal bool IgnoreDrawOff;

        //
        public CompProperties_FX Props => (CompProperties_FX)props;
        
        //
        public CompPowerTrader ParentPowerComp { get; private set; }
        public FXDefExtension GraphicExtension { get; private set; }
        public List<FXLayer> Overlays { get; private set; }
        
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            GraphicExtension = parent.def.FXExtension();
            
            ParentPowerComp = parent.TryGetComp<CompPowerTrader>();

            //Resolve FX Parents
            ResolveFXHolders();

            //Setup Layers
            //Init Data On First Spawn
            if (!spawnedOnce)
            {
                if (!Props.fxLayers.NullOrEmpty())
                {
                    Overlays = new List<FXLayer>();
                    var spawnTick = Find.TickManager.TicksGame;
                    var tickOffset = Props.tickOffset.RandomInRange;
                    for (int i = 0; i < Props.fxLayers.Count; i++)
                    {
                        Overlays.Add(new FXLayer(Props.fxLayers[i], new FXParentInfo(tickOffset, spawnTick, GraphicExtension, parent), i));
                    }
                }
            }

            //
            spawnedOnce = true;
        }
        
        public override void PostExposeData()
        {
            base.PostExposeData();
        }

        private void ResolveFXHolders()
        {
            allHeldFXComps ??= new List<IFXHolder>();

            //Add parent if it implements the FX interface
            if (parent is IFXHolder parentFX)
            {
                allHeldFXComps.Add(parentFX);
                PopulateEvents(parentFX);
            }

            //Populate Events
            foreach (var comp in parent.AllComps)
            {
                if (comp is IFXHolder compFX)
                {
                    allHeldFXComps.Add(compFX);
                    PopulateEvents(compFX);
                }
            }
        }

        private void PopulateEvents(IFXHolder fxHolder)
        {
            GetPowerProvider += fxHolder.FX_PowerProviderFor;
            GetShouldDraw += fxHolder.FX_ShouldDraw;
            GetOpacity += fxHolder.FX_GetOpacity;
            GetRotation += fxHolder.FX_GetRotation;
            GetAnimationSpeed += fxHolder.FX_GetAnimationSpeedFactor;
            GetColor += fxHolder.FX_GetColor;
            GetDrawPosition += fxHolder.FX_GetDrawPosition;
            GetAction += fxHolder.FX_GetAction;

            GetShouldThrowEffects += fxHolder.FX_ShouldThrowEffects;
            EffectSpawned += fxHolder.FX_OnEffectSpawned;
        }
        
        //Notification
        public override void ReceiveCompSignal(string signal)
        {
            if (!parent.Spawned) return;
            if (signal is "PowerTurnedOn" or "PowerTurnedOff" or "FlickedOn" or "FlickedOff" or "Refueled" or "RanOutOfFuel" or "ScheduledOn" or "ScheduledOff")
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);
            }
        }

        public override void CompTick()
        {
            Tick();
        }

        public override void CompTickRare()
        {
            for (int i = 0; i < 750; i++)
            {
                Tick();
            }
        }

        private void Tick()
        {
            //Update Graphics
            for (var i = 0; i < Overlays.Count; i++)
            {
                var g = Overlays[i];
                g.Tick();
            }
        }

        //Drawing
        private bool CanDraw(FXLayerArgs args)
        {
            if (Overlays[args].data.skip) 
                return false;
            if (!DrawBool(args) || OpacityFloat(args) <= 0) 
                return false;
            if (!HasPower(args))
                return false;
            return true;
        }

        private bool HasPower(FXLayerArgs args)
        {
            if (Overlays[args].data.needsPower)
            {
                var provider = PowerProvider(args);
                if (provider is {PowerOn: true})
                    return true;
                if (ParentPowerComp is {PowerOn: true}) 
                    return true;
                return false;
            }
            return true;
        }
        
        //TODO: Events with value returns always go through all delegates and return the last value
        //Layer Data Getters
        public CompPowerTrader PowerProvider(FXLayerArgs args)
        {
            return GetPowerProvider.Invoke(args);
        }
        
        public bool DrawBool(FXLayerArgs args)
        {
            return GetShouldDraw.Invoke(args);
        }

        public float OpacityFloat(FXLayerArgs args)
        {
            return GetOpacity.Invoke(args);
        }

        public float? ExtraRotation(FXLayerArgs args)
        {
            return GetRotation.Invoke(args);
        }

        public float? AnimationSpeed(FXLayerArgs args)
        {
            return GetAnimationSpeed.Invoke(args);
        }
        
        public Color? ColorOverride(FXLayerArgs args)
        {
            return GetColor.Invoke(args);
        }

        public Vector3? DrawPosition(FXLayerArgs args)
        {
            return GetDrawPosition.Invoke(args);
        }

        public Action<FXLayer> Action(FXLayerArgs args)
        {
            return GetAction.Invoke(args);
        }

        public bool ShouldThrowEffects(EffecterLayerArgs args)
        {
            return GetShouldThrowEffects.Invoke(args);
        }
        
        public void OnEffectSpawned(FXEffecterArgs args)
        {
            EffectSpawned.Invoke(args);
        }
        //
        public void DrawCarried(Vector3 loc)
        {
            for (int i = 0; i < Overlays.Count; i++)
            {
                var args = Overlays[i].GetArgs();
                if (Overlays[i].data.fxMode != FXMode.Static && CanDraw(args))
                {
                    var drawPos = DrawPosition(args);
                    var diff = drawPos - parent.DrawPos;
                    Overlays[i].Draw(loc + diff);
                }
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            for (int i = 0; i < Overlays.Count; i++)
            {
                if (Overlays[i].data.fxMode != FXMode.Static && CanDraw(Overlays[i].GetArgs()))
                {
                    Overlays[i].Draw();
                }
            }
        }

        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);
            for (int i = 0; i < Overlays.Count; i++)
            {
                if (Overlays[i].data.fxMode == FXMode.Static && CanDraw(Overlays[i].GetArgs()))
                {
                    Overlays[i].Print(layer);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action()
            {
                defaultLabel = "No DrawOffset",
                action = () =>
                {
                    IgnoreDrawOff = !IgnoreDrawOff;
                    parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Buildings);
                }
            };
        }
    }
}