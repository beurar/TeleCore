using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class CompFX : ThingComp
    {
        //
        private IFXObject internalMainFXParent;
        private IFXObject parentFXThing;
        private List<IFXObject> parentFXComps;

        private IFXObject[] IFXPPL; //Parent Per Layer

        //
        private FXDefExtension extensionInt;

        //Layers
        private List<FXGraphic> mainLayers;

        private int spawnTick = 0;
        private int tickOffset = 0;
        private bool spawnedOnce = false;
   
        public int Size => Overlays.Count;
        public int TickOffset => tickOffset;
        public int SpawnTick => spawnTick;

        public List<FXGraphic> Overlays => mainLayers;

        //
        public CompProperties_FX Props => base.props as CompProperties_FX;
        public CompPowerTrader CompPower => MainParent == null ? parent.TryGetComp<CompPowerTrader>() : (MainParent.ForcedPowerComp == null ? parent.TryGetComp<CompPowerTrader>() : (CompPowerTrader)MainParent.ForcedPowerComp);
        public CompPowerPlant CompPowerPlant => parent.TryGetComp<CompPowerPlant>();

        public FXDefExtension GraphicExtension => extensionInt ??= parent.def.FXExtension();

        public IFXObject MainParent => internalMainFXParent;
        public IFXObject IParentThing => parentFXThing;
        public List<IFXObject> IParentComps => parentFXComps;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref spawnTick, "spawnTick");
            Scribe_Values.Look(ref tickOffset, "tickOffset");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            //Resolve FX Parents
            ResolveFXParents();

            //Setup Layers
            //Init Data On First Spawn
            if (!spawnedOnce)
            {
                if (!Props.overlays.NullOrEmpty())
                {
                    mainLayers = new();
                    for (int i = 0; i < Props.overlays.Count; i++)
                    {
                        mainLayers.Add(new FXGraphic(this, Props.overlays[i], i));
                    }
                }
            }

            if (!respawningAfterLoad)
            {
                spawnTick = Find.TickManager.TicksGame;
                tickOffset = Props.tickOffset.RandomInRange;
            }

            //
            spawnedOnce = true;
        }

        private void ResolveFXParents()
        {
            var fx = new List<IFXObject>();

            //Get FX Parents
            if (parent is IFXObject parentFX)
            {
                parentFXThing = parentFX;
                fx.Add(parentFXThing);
            }

            foreach (var comp in parent.AllComps)
            {
                if (comp is IFXObject compFX)
                {
                    parentFXComps ??= new List<IFXObject>();
                    parentFXComps.Add(compFX);
                    fx.Add(compFX);
                }
            }

            //
            fx = fx.OrderBy(f => f.Priority).ToList();
            
            foreach (var fxObject in fx)
            {
                if (fxObject.IsMain)
                    internalMainFXParent = fxObject;
            }

            if (internalMainFXParent == null && fx.Count > 0)
            {
                internalMainFXParent = fx.First();
            }

            //Resolve Order
            var fxPerLayer = new IFXObject[Props.overlays.Count];
            for (int i = 0; i < Props.overlays.Count; i++)
            {
                foreach (var fxObject in fx)
                {
                    if (fxObject.FX_AffectsLayerAt(i))
                    {
                        fxPerLayer[i] = fxObject;
                        break;
                    }
                }
            }

            IFXPPL = fxPerLayer;
            for (var i = 0; i < IFXPPL.Length; i++)
            {
                var fxObject = IFXPPL[i];
            }
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
        private bool CanDraw(int index)
        {
            if (Overlays[index].data.skip)
                return false;
            if (!DrawBool(index) || OpacityFloat(index) <= 0)
                return false;
            if (!HasPower(index))
                return false;
            return true;
        }

        private bool HasPower(int index)
        {
            if (Overlays[index].data.needsPower)
            {
                if (CompPowerPlant != null)
                    return CompPowerPlant.PowerOutput > 0;
                else
                if (CompPower != null)
                    return CompPower.PowerOn;
            }
            return true;
        }

        //Layer Properties
        public bool DrawBool(int index)
        {
            if (MainParent == null) return true;
            return IFXPPL[index].FX_ShouldDrawAt(index);
        }

        public float OpacityFloat(int index)
        {
            if (MainParent == null) return 1f;
            return IFXPPL[index].FX_GetOpacityAt(index);
        }

        public float? RotationOverride(int index)
        {
            if (MainParent == null) return 0;
            return IFXPPL[index].FX_GetRotationAt(index);
        }

        public float? GetRotationSpeedOverride(int index)
        {
            if (MainParent == null) return 0;
            return IFXPPL[index].FX_GetRotationSpeedAt(index);
        }

        public Color? ColorOverride(int index)
        {
            if (MainParent == null) return Color.white;
            return IFXPPL[index].FX_GetColorAt(index);
        }

        public Vector3? DrawPosition(int index)
        {
            if (MainParent == null) return parent.DrawPos;
            return IFXPPL[index].FX_GetDrawPositionAt(index);
        }

        public Action<FXGraphic> Action(int index)
        {
            if (MainParent == null) return null;
            return IFXPPL[index].FX_GetActionAt(index);
        }

        //
        public void DrawCarried(Vector3 loc)
        {
            for (int i = 0; i < Overlays.Count; i++)
            {
                if (Overlays[i].data.mode != FXMode.Static && CanDraw(i))
                {
                    var drawPos = DrawPosition(i);
                    var diff = drawPos - parent.DrawPos;
                    Overlays[i].Draw(loc + diff); //DrawPosition(i), parent.Rotation, RotationOverride(i), Action(i), i
                }
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            for (int i = 0; i < Overlays.Count; i++)
            {
                if (Overlays[i].data.mode != FXMode.Static && CanDraw(i))
                {
                    Overlays[i].Draw(); //DrawPosition(i), parent.Rotation, RotationOverride(i), Action(i), i
                }
            }
        }

        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);
            for (int i = 0; i < Overlays.Count; i++)
            {
                if (Overlays[i].data.mode == FXMode.Static && CanDraw(i))
                {
                    Overlays[i].Print(layer); //, DrawPosition(i), parent.Rotation, RotationOverride(i), parent
                }
            }
        }

        public bool IgnoreDrawOff;

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