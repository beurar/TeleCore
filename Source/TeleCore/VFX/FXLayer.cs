using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class FXLayer
    {
        //Cached
        private Graphic? _graphicInt;
        private Material? _drawMat;
        private MaterialPropertyBlock _materialProperties;

        //FX
        private readonly bool _inactive;
        private readonly int _index = 0;
        private readonly int renderPriority;
        private readonly float _altitude;
        
        //Effecter
        private EffecterLayer? _effecterLayer;
        private readonly bool _isEffecterLayer;

        //Data
        public readonly FXLayerData data;
        private readonly FXParentInfo parentInfo;

        //Dynamic Working Data
        private Vector2 drawSize = Vector2.one;
        private Color drawColor = Color.white;
        private Mesh drawMesh;
        private float exactRotation;
        private bool flipUV;
        private int ticksToBlink = 0;
        private int blinkDuration = 0;

        public MaterialPropertyBlock PropertyBlock => _materialProperties;
        
        public CompFX CompFX { get; }
        public FXLayerArgs Args { get; }
        
        //
        public int Index => _index;
        public int RenderPriority => renderPriority;

        public bool HasEffecter => _effecterLayer != null;
        
        private Rot4 ParentRot4 => parentInfo.ParentThing.Rotation;
        
        //Rotation
        public float TrueRotation => CompFX.GetExtraRotation(Args) + exactRotation;
        public Vector3 DrawPos => CompFX.GetDrawPositionOverride(Args) ?? CompFX.parent.DrawPos;
        private float RotationSpeedPerTick => AnimationSpeed * (CompFX.GetRotationSpeedOverride(Args) ?? (data.rotate?.rotationSpeed ?? 0));
        
        
        private Color ColorOverride => CompFX.GetColorOverride(Args) ?? Color.white;
        private float Opacity => CompFX.GetOpacityFloat(Args);


        private float AnimationSpeed => CompFX.GetAnimationSpeedFactor(Args);
        private Action<RoutedDrawArgs> DrawAction => CompFX.GetDrawAction(Args);

        private bool ShouldDoEffecter => CompFX.ShouldThrowEffects(Args);
        private bool HasPower => CompFX.HasPower(Args);
        
        //Blink
        public bool ShouldBeBlinkingNow => blinkDuration > 0;
        
        
        public FXLayer(CompFX compFX, FXLayerData data, FXParentInfo info, int index)
        {
            TLog.Message($"Adding Layer {index}: {data.graphicData?.texPath} ({data.fxMode})");
            
            //
            _materialProperties = new MaterialPropertyBlock();
            
            //
            this.data = data;
            _index = index;
            parentInfo = info;
            renderPriority = data.renderPriority ?? index;
            
            CompFX = compFX;
            
            if (data.skip)
            {
                _inactive = true;
                return;
            }

            //Generate Effecter Layer
            if(data.effecterDef != null)
            {
                _effecterLayer = new EffecterLayer(data.effecterDef);
            }
            
            //Generate Visual Layer
            if (data.graphicData == null)
            {
                _isEffecterLayer = _effecterLayer != null;
            }
            else
            {
                _altitude = (data.altitude ?? info.Def.altitudeLayer).AltitudeFor();
                if (data.rotate != null)
                {
                    exactRotation = data.rotate.startRotation.RandomInRange;
                }
                if (data.drawLayer != null)
                {
                    _altitude += (data.drawLayer.Value * Altitudes.AltInc);
                }
                else
                {
                    _altitude += ((index + 1) * Altitudes.AltInc);
                }
            }

            //Set Args Cache
            Args = this.GetArgs();
        }

        public void TickLayer(int tickInterval)
        {
            if (_inactive || !HasPower) return;
            
            if (ShouldDoEffecter)
            {
                _effecterLayer?.Tick(parentInfo.ParentThing, parentInfo.ParentThing);
            }
            
            //Skip rest if its just the effect
            if (_isEffecterLayer) return;
            var tick = Find.TickManager.TicksGame;

            //Rotate
            if (RotationSpeedPerTick > 0)
                exactRotation += RotationSpeedPerTick * StaticData.DeltaTime;

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
                {
                    ResetBlink();
                }
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
            var opaVal = TMath.OscillateBetween(fade.opacityRange.min, fade.opacityRange.max, fade.opacityDuration, tick + parentInfo.TickOffset + fade.initialOpacityOffset);
            drawColor.a = opaVal;
        }

        private void TryTickSize(int tick)
        {
            if (data.resize == null) return;
            var resize = data.resize;
            if (resize.sizeRange.Average <= 0) return;
            var sizeVal = TMath.OscillateBetween(resize.sizeRange.min, resize.sizeRange.max, resize.sizeDuration, tick + parentInfo.TickOffset + resize.initialSizeOffset);
            drawSize *= sizeVal;
        }

        public Graphic Graphic
        {
            get
            {
                if (_graphicInt == null)
                {
                    if (parentInfo.ParentThing.Graphic is Graphic_Random random)
                    {
                        var path = this.data.graphicData.texPath;
                        var parentName = random.SubGraphicFor(parentInfo.ParentThing).path.Split('/').Last();
                        var lastPart = path.Split('/').Last();
                        path += "/" + lastPart;
                        path += "_" + parentName.Split('_').Last();
                        _graphicInt = GraphicDatabase.Get(typeof(Graphic_Single), path, data.graphicData.shaderType.Shader, data.graphicData.drawSize, data.graphicData.color, data.graphicData.colorTwo);
                    }
                    else if (data.graphicData != null)
                    {
                        _graphicInt = data.graphicData.Graphic;
                    }

                    if (!data.textureParams.NullOrEmpty())
                    {
                        foreach (var param in data.textureParams)
                        {
                            param.ApplyOn(_graphicInt);
                        }
                    }
                }
                return _graphicInt;
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
            if (_isEffecterLayer) return;
            
            //
            var drawPos = drawLocOverride ?? DrawPos;
            GetDrawInfo(Graphic, ref drawPos, ParentRot4, parentInfo.Extension, parentInfo.ParentThing.def, out drawSize, out _drawMat, out drawMesh, out float extraRotation, out flipUV);

            if(CompFX.IgnoreDrawOff)
                drawPos += data.drawOffset;
            
            //Colors
            var graphicColor = data.graphicData.color;
            if (ColorOverride != Color.white)
                graphicColor *= ColorOverride;

            graphicColor.a = Opacity;
            graphicColor *= drawColor;

            //
            _drawMat.SetTextureOffset("_MainTex", data.texCoords.position);
            _drawMat.SetTextureScale("_MainTex", data.texCoords.size);

            _materialProperties.SetColor(ShaderPropertyIDs.Color, graphicColor);

            var rotationQuat = TrueRotation.ToQuat();

            if (data.PivotOffset != null)
            {
                var pivotPoint = drawPos + data.PivotOffset.Value;
                Vector3 relativePos = rotationQuat * (drawPos - pivotPoint);
                drawPos = pivotPoint + (relativePos);
            }

            //
            DrawAction?.Invoke(new RoutedDrawArgs
            {
                graphic = Graphic,
                drawPos = DrawPos,
                altitude = _altitude,
                rotation = TrueRotation,
                mesh = drawMesh
            });

            //
            Graphics.DrawMesh(drawMesh, new Vector3(drawPos.x, _altitude, drawPos.z), rotationQuat, _drawMat, 0, null, 0, _materialProperties);
        }

        public void Print(SectionLayer layer)
        {
            if (_isEffecterLayer) return;
            
            //
            var drawPos = DrawPos;
            GetDrawInfo(Graphic, ref drawPos, ParentRot4, parentInfo.Extension, parentInfo.ParentThing.def, out drawSize, out _drawMat, out drawMesh, out float extraRotation, out flipUV);
            if (!CompFX.IgnoreDrawOff)
                drawPos += data.drawOffset;
            Printer_Plane.PrintPlane(layer, new Vector3(drawPos.x, _altitude, drawPos.z), drawSize, _drawMat, TrueRotation, flipUV);
        }
    }
}
