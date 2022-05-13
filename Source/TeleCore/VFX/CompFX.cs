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
    public class CompFX : ThingComp
    {
        //
        private IFXObject iParent;
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
        public CompPowerTrader CompPower => IParent == null ? parent.TryGetComp<CompPowerTrader>() : (IParent.ForcedPowerComp == null ? parent.TryGetComp<CompPowerTrader>() : (CompPowerTrader)IParent.ForcedPowerComp);
        public CompPowerPlant CompPowerPlant => parent.TryGetComp<CompPowerPlant>();

        public FXDefExtension GraphicExtension => extensionInt ??= parent.def.Tele().graphics;

        public IFXObject IParent
        {
            get
            {
                if (iParent != null) return iParent;
                if (!Props.useParentClass && parent.AllComps.Any(c => c is IFXObject))
                {
                    iParent = parent.AllComps.First(x => x is IFXObject) as IFXObject;
                    return iParent;
                }
                return iParent ??= parent as IFXObject;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref spawnTick, "spawnTick");
            Scribe_Values.Look(ref tickOffset, "tickOffset");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            //Setup Layers
            //Init Data On First Spawn -
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
        public float OpacityFloat(int index)
        {
            if (IParent == null || IParent.OpacityFloats.Count() < (index + 1))
            {
                return 1f;
            }
            return IParent.OpacityFloats[index];
        }

        public float? RotationOverride(int index)
        {
            if (IParent == null || IParent.RotationOverrides.Count() < (index + 1))
            {
                return null;
            }
            return IParent.RotationOverrides[index];
        }

        public float? MoveSpeed(int index)
        {
            if (IParent == null || IParent.MoveSpeeds == null || IParent.MoveSpeeds.Count() < (index + 1))
            {
                return 1f;
            }
            return IParent.MoveSpeeds[index];
        }

        public bool DrawBool(int index)
        {
            if (IParent == null || IParent.DrawBools.Count() < (index + 1))
            {
                return true;
            }
            return IParent.DrawBools[index];
        }

        //
        public Color ColorOverride(int index)
        {
            if (IParent == null || IParent.ColorOverrides.Count() < (index + 1))
            {
                return Color.white;
            }
            //
            return IParent.ColorOverrides[index] ?? Color.white;
        }

        public Vector3 DrawPosition(int index)
        {
            if (IParent == null || IParent.DrawPositions.Count() < (index + 1))
            {
                return parent.DrawPos;
            }
            //
            var val = IParent.DrawPositions[index];
            if (val.HasValue)
                return val.Value;

            return parent.DrawPos;
        }

        public Action<FXGraphic> Action(int index)
        {
            if (IParent?.Actions == null || IParent.Actions.Count() < (index + 1))
            {
                return null;
            }
            return IParent.Actions[index];
        }

        public Vector2 TextureOffset => (bool)IParent?.TextureOffset.HasValue ? IParent.TextureOffset.Value : Vector2.zero;
        public Vector2 TextureScale => (bool)IParent?.TextureScale.HasValue ? IParent.TextureScale.Value : new Vector2(1, 1);

        //
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
    }
}