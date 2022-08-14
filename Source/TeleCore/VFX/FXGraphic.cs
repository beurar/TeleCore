using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class FXGraphic
    {
        //Loaded fixed Data
        private CompFX parent;
        public Graphic graphicInt;
        public FXGraphicData data;
        public float altitude;
        public int index = 0;

        private bool unused;
        private Material drawMat;

        //FXModes
        public int ticksToBlink = 0;
        public int blinkDuration = 0;

        //Dynamic Working Data
        Vector2 drawSize = Vector2.one;
        private Mesh drawMesh;
        private Color drawColor;
        private float exactRotation;
        private bool flipUV;

        private readonly MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();

        public MaterialPropertyBlock PropertyBlock => materialProperties;

        public float ExactRotation => exactRotation;

        //Parent Values
        private FXDefExtension ExData => parent.GraphicExtension;
        private Rot4 Rot4 => parent.parent.Rotation;
        private float GetOpacity => parent.OpacityFloat(index);
        private float GetRotation => (parent.RotationOverride(index) ?? 0) + exactRotation;
        private float GetRotationSpeed => (parent.GetRotationSpeedOverride(index) ?? 1) * RotationSpeed;
        private Color GetColorOverride => parent.ColorOverride(index) ?? Color.white;
        private Vector3 GetDrawPos => parent.DrawPosition(index) ?? parent.parent.DrawPos;
        private Action<FXGraphic> GetAction => parent.Action(index);

        //Blink
        public bool ShouldBeBlinkingNow => blinkDuration > 0;

        //Fade

        //Rotate
        public float RotationSpeed => data.rotate?.rotationSpeed ?? 0;

        public FXGraphic(CompFX parent, FXGraphicData data, int index)
        {
            TLog.Message($"Adding Layer {index}: {data.data?.texPath} ({data.mode})");
            this.parent = parent;
            this.data = data;
            this.index = index;
            
            if (data.skip)
            {
                unused = true;
                return;
            }

            //
            drawColor = Color.white;
            if (data.rotate != null)
            {
                exactRotation = data.rotate.startRotation.RandomInRange;
            }
            altitude = (data.altitude ?? parent.parent.def.altitudeLayer).AltitudeFor();
            if (data.drawLayer != null)
                altitude += (data.drawLayer.Value * Altitudes.AltInc);
            else
                altitude += ((index + 1) * Altitudes.AltInc);
        }

        public void Tick()
        {
            if (unused) return;
            var tick = Find.TickManager.TicksGame;

            //Rotate
            if (GetRotationSpeed != 0)
                exactRotation += GetRotationSpeed * StaticData.DeltaTime;

            //Blink
            TryTickBlink(tick);

            //Fade
            TryTickFade(tick);

            //Resize
            TryTickSize(tick);
        }

        private void TryTickBlink(int tick)
        {
            if (data.blink == null) return;
            if (ticksToBlink > 0 && blinkDuration == 0)
            {
                drawColor.a = 0;
            }
            else
            {
                if (blinkDuration > 0)
                {
                    drawColor.a = 1;
                    blinkDuration--;
                }
                else
                    ResetBlink();
            }
        }

        private void ResetBlink()
        {
            ticksToBlink = data.blink.interval;
            blinkDuration = data.blink.duration;
        }

        private void TryTickFade(int tick)
        {
            if (data.fade == null) return;
            var fade = data.fade;
            if (fade.opacityRange.Average <= 0) return;
            var opaVal = TMath.OscillateBetween(fade.opacityRange.min, fade.opacityRange.max, fade.opacityDuration, tick + parent.TickOffset + fade.initialOpacityOffset);
            drawColor.a = opaVal;
        }

        private void TryTickSize(int tick)
        {
            if (data.resize == null) return;
            var resize = data.resize;
            if (resize.sizeRange.Average <= 0) return;
            var sizeVal = TMath.OscillateBetween(resize.sizeRange.min, resize.sizeRange.max, resize.sizeDuration, tick + parent.TickOffset + resize.initialSizeOffset);
            drawSize *= sizeVal;
        }

        public Graphic Graphic
        {
            get
            {
                if (graphicInt == null)
                {
                    if (parent.parent.Graphic is Graphic_Random random)
                    {
                        var path = this.data.data.texPath;
                        var parentName = random.SubGraphicFor(parent.parent).path.Split('/').Last();
                        var lastPart = path.Split('/').Last();
                        path += "/" + lastPart;
                        path += "_" + parentName.Split('_').Last();
                        graphicInt = GraphicDatabase.Get(typeof(Graphic_Single), path, data.data.shaderType.Shader, data.data.drawSize, data.data.color, data.data.colorTwo);
                    }
                    else if (data.data != null)
                    {
                        graphicInt = data.data.Graphic;
                    }

                    if (!data.textureParams.NullOrEmpty())
                    {
                        foreach (var param in data.textureParams)
                        {
                            param.ApplyOn(graphicInt);
                        }
                    }
                }
                return graphicInt;
            }
        }

        internal static void GetDrawInfo(Graphic g, ref Vector3 inoutPos, Rot4 rot, FXDefExtension exData, ThingDef def, out Vector2 drawSize, out Material drawMat, out Mesh drawMesh, out float extraRotation, out bool flipUV)
        {
            drawMat = g.MatAt(rot);

            //DrawPos
            if ((exData?.alignToBottom ?? false) && def != null)
            {
                //Align to bottom
                float height = g.drawSize.y;
                float selectHeight = def.size.z;
                float diff = height - selectHeight;
                inoutPos.z += diff / 2;
            }

            inoutPos += g.data.drawOffset; //exData?.drawOffset ?? Vector3.zero;
            //DrawSize
            drawSize = g.drawSize;
            bool drawRotated = exData?.drawRotatedOverride ?? g.ShouldDrawRotated;
            if (drawRotated)
            {
                flipUV = false;
            }
            else
            {
                if (rot.IsHorizontal && (exData?.rotateDrawSize ?? true))
                {
                    drawSize = drawSize.Rotated();
                }
                flipUV = /*!g.ShouldDrawRotated &&*/ ((rot == Rot4.West && g.WestFlipped) || (rot == Rot4.East && g.EastFlipped));
            }
            drawMesh = flipUV ? MeshPool.GridPlaneFlip(drawSize) : MeshPool.GridPlane(drawSize);

            //Set rotation
            if (!drawRotated)
            {
                extraRotation = 0;
                return;
            }
            float num = rot.AsAngle;
            num += g.DrawRotatedExtraAngleOffset;
            if ((rot == Rot4.West && g.WestFlipped) || (rot == Rot4.East && g.EastFlipped))
            {
                num += 180f;
            }
            extraRotation = num;
        }

        //[TweakValue("FXLayer", 0, 10f)]
        //private static int CurrentLayer = 0;

        public void Draw(Vector3? drawLocOverride = null)
        {
            //Pre-Action
            GetAction?.Invoke(this);
            
            //
            var drawPos = drawLocOverride ?? GetDrawPos;
            GetDrawInfo(Graphic, ref drawPos, Rot4, ExData, parent.parent.def, out drawSize, out drawMat, out drawMesh, out float extraRotation, out flipUV);

            if(!parent.IgnoreDrawOff)
                drawPos += data.drawOffset;

            //Colors
            var graphicColor = data.data.color;
            if (GetColorOverride != Color.white)
                graphicColor *= GetColorOverride;

            graphicColor.a = GetOpacity;
            graphicColor *= drawColor;

            //
            drawMat.SetTextureOffset("_MainTex", data.texCoords.position);
            drawMat.SetTextureScale("_MainTex", data.texCoords.size);

            materialProperties.SetColor(ShaderPropertyIDs.Color, graphicColor);

            var rotationQuat = (GetRotation + extraRotation).ToQuat();

            if (data.PivotOffset != null)
            {
                var pivotPoint = drawPos + data.PivotOffset.Value;
                Vector3 relativePos = rotationQuat * (drawPos - pivotPoint);
                drawPos = pivotPoint + (relativePos);
            }

            Graphics.DrawMesh(drawMesh, new Vector3(drawPos.x, altitude, drawPos.z), rotationQuat, drawMat, 0, null, 0, materialProperties);
        }

        public void Print(SectionLayer layer)
        {
            //
            var drawPos = GetDrawPos;
            GetDrawInfo(Graphic, ref drawPos, Rot4, ExData, parent.parent.def, out drawSize, out drawMat, out drawMesh, out float extraRotation, out flipUV);
            if (!parent.IgnoreDrawOff)
                drawPos += data.drawOffset;
            Printer_Plane.PrintPlane(layer, new Vector3(drawPos.x, altitude, drawPos.z), drawSize, drawMat, (GetRotation + extraRotation), flipUV);
        }
    }
}
