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
        public float Rotation => exactRotation;


        //Parent Values
        private FXDefExtension ExData => parent.GraphicExtension;
        private Rot4 Rot4 => parent.parent.Rotation;
        private float GetOpacity => parent.OpacityFloat(index);
        private float GetRotation => parent.RotationOverride(index) ?? 0;
        private float GetMoveSpeed => parent.MoveSpeed(index) ?? 1;
        private Color GetColorOverride => parent.ColorOverride(index);
        private Vector3 GetDrawPos => parent.DrawPosition(index);
        private Action<FXGraphic> GetAction => parent.Action(index);

        //Move
        public float MoverSpeed => data.move.MoverSpeed;

        //Blink

        //Fade

        //Rotate
        public float RotationSpeed => data.rotate.rotationSpeed;

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
            exactRotation = data.rotate.startRotation;
            if (data.drawLayer.HasValue)
                altitude = parent.parent.def.altitudeLayer.AltitudeFor() + (data.drawLayer.Value * Altitudes.AltInc);
            else
                altitude = parent.parent.def.altitudeLayer.AltitudeFor() + ((index + 1) * Altitudes.AltInc);
        }

        public void Tick()
        {
            if (unused) return;
            //Rotate
            if (RotationSpeed != 0)
                exactRotation += (GetMoveSpeed * (RotationSpeed * 0.0166666675f));

            //Blink
            TryTickBlink();

            //Fade
            TryTickFade();
        }

        private void TryTickBlink()
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

        private void TryTickFade()
        {
            if (data.fade == null) return;
            var tick = Find.TickManager.TicksGame;
            var fade = data.fade;
            var opaVal = TMath.OscillateBetween(fade.opacityRange.min, fade.opacityRange.max, fade.opacityDuration, tick + parent.TickOffset);
            if (fade.opacityRange != FloatRange.Zero)
                drawColor.a = opaVal;
        }

        private void TryTickSize()
        {
            //var sizeVal = TeleMath.OscillateBetween(pulse.sizeRange.min, pulse.sizeRange.max, pulse.sizeDuration, tick + parent.tickOffset);
            //if (fade.sizeRange != FloatRange.Zero)
                //drawSize *= sizeVal;
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

        public void Draw()
        {
            //Pre-Action
            GetAction?.Invoke(this);
            
            //
            var drawPos = GetDrawPos;
            GetDrawInfo(Graphic, ref drawPos, Rot4, ExData, parent.parent.def, out drawSize, out drawMat, out drawMesh, out float extraRotation, out flipUV);
            drawPos += data.drawOffset;

            var graphicColor = data.data.color;
            graphicColor *= drawColor;
            graphicColor.a = GetOpacity;
            if (GetColorOverride != Color.white)
                graphicColor *= GetColorOverride;

            drawMat.SetTextureOffset("_MainTex", parent.TextureOffset);
            drawMat.SetTextureScale("_MainTex", parent.TextureScale);

            materialProperties.SetColor(ShaderPropertyIDs.Color, graphicColor);

            var rotationQuat = (exactRotation + extraRotation + GetRotation).ToQuat();

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
            drawPos += data.drawOffset;
            Printer_Plane.PrintPlane(layer, new Vector3(drawPos.x, altitude, drawPos.z), drawSize, drawMat, (GetRotation + exactRotation) + extraRotation, flipUV);
        }
    }
}
