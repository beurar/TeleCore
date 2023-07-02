using System;
using TeleCore.Data.Events;
using UnityEngine;
using Verse;

namespace TeleCore;

public class FXLayer
{
    private readonly float _altitude;

    //FX
    private readonly bool _inactive;

    //Data
    public readonly FXLayerData data;
    private readonly FXParentInfo parentInfo;

    private Material? _drawMat;

    //Cached
    private Graphic? _graphicInt;
    private int blinkDuration;
    private Color drawColor = Color.white;
    private Mesh drawMesh;

    //Dynamic Working Data
    private Vector2 drawSize = Vector2.one;
    private float exactRotation;
    private bool flipUV;
    private int ticksToBlink;


    public FXLayer(CompFX compFX, FXLayerData data, FXParentInfo info, int index)
    {
        PropertyBlock = new MaterialPropertyBlock();

        this.data = data;
        Index = index;
        parentInfo = info;
        RenderPriority = data.renderPriority ?? index;

        CompFX = compFX;

        if (data.skip)
        {
            _inactive = true;
            Args = this.GetArgs();
            return;
        }

        _altitude = (data.altitude ?? info.Def.altitudeLayer).AltitudeFor();
        if (data.rotate != null) exactRotation = data.rotate.startRotation.RandomInRange;

        if (data.drawLayer != null)
            _altitude += data.drawLayer.Value * Altitudes.AltInc;
        else
            _altitude += (index + 1) * Altitudes.AltInc;

        //Set Args Cache
        Args = this.GetArgs();
    }

    public MaterialPropertyBlock PropertyBlock { get; }

    public CompFX CompFX { get; }
    public FXLayerArgs Args { get; }

    //
    public int Index { get; }

    public int RenderPriority { get; }

    private Rot4 ParentRot4 => parentInfo.ParentThing.Rotation;

    //Rotation
    public float TrueRotation => CompFX.GetExtraRotation(Args) + exactRotation;
    public Vector3 DrawPos => CompFX.GetDrawPositionOverride(Args) ?? CompFX.parent.DrawPos;

    private float RotationSpeedPerTick => AnimationSpeedFactor *
                                          (CompFX.GetRotationSpeedOverride(Args) ?? (data.rotate?.rotationSpeed ?? 0));


    private Color ColorOverride => CompFX.GetColorOverride(Args) ?? Color.white;
    private float Opacity => CompFX.GetOpacityFloat(Args);


    private float AnimationSpeedFactor => CompFX.GetAnimationSpeedFactor(Args);
    private Func<RoutedDrawArgs, bool> DrawFunction => CompFX.GetDrawFunction(Args);

    private bool HasPower => CompFX.HasPower(Args);

    //Blink
    public bool ShouldBeBlinkingNow => blinkDuration > 0;

    public Graphic Graphic
    {
        get
        {
            if (_graphicInt == null)
            {
                if (data.graphicData != null)
                {
                    _graphicInt = data.graphicData.Graphic;
                    if (_graphicInt is Graphic_Selectable selectable)
                        _graphicInt = selectable.AtIndex(CompFX.GetSelectedIndex(Args));
                }

                if (!data.textureParams.NullOrEmpty())
                    foreach (var param in data.textureParams)
                        param.ApplyOn(_graphicInt);
            }

            return _graphicInt;
        }
    }

    public void TickLayer(int tickInterval)
    {
        if (_inactive || !HasPower) return;

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
        var opaVal = TMath.OscillateBetween(fade.opacityRange.min, fade.opacityRange.max, fade.opacityDuration,
            tick + parentInfo.TickOffset + fade.initialOpacityOffset);
        drawColor.a = opaVal;
    }

    private void TryTickSize(int tick)
    {
        if (data.resize == null) return;
        var resize = data.resize;
        if (resize.sizeRange.Average <= 0) return;
        var sizeVal = TMath.OscillateBetween(resize.sizeRange.min, resize.sizeRange.max, resize.sizeDuration,
            tick + parentInfo.TickOffset + resize.initialSizeOffset);
        drawSize *= sizeVal;
    }

    internal static void GetDrawInfo(Graphic g, Thing thing, ThingDef def, ref Vector3 inoutPos, Rot4 rot,
        FXDefExtension exData, out Vector2 drawSize, out Material drawMat, out Mesh drawMesh, out float extraRotation,
        out bool flipUV)
    {
        drawMat = g.MatAt(rot, thing);

        //DrawPos
        if ((exData?.alignToBottom ?? false) && def != null)
        {
            //Align to bottom
            var height = g.drawSize.y;
            float selectHeight = def.size.z;
            var diff = height - selectHeight;
            inoutPos.z += diff / 2;
        }

        inoutPos += g.data.drawOffset; //exData?.drawOffset ?? Vector3.zero;
        //DrawSize
        drawSize = g.drawSize;
        var drawRotated = exData?.drawRotatedOverride ?? g.ShouldDrawRotated;
        if (drawRotated)
        {
            flipUV = false;
        }
        else
        {
            if (rot.IsHorizontal && (exData?.rotateDrawSize ?? true)) drawSize = drawSize.Rotated();
            flipUV = /*!g.ShouldDrawRotated &&*/
                (rot == Rot4.West && g.WestFlipped) || (rot == Rot4.East && g.EastFlipped);
        }

        drawMesh = flipUV ? MeshPool.GridPlaneFlip(drawSize) : MeshPool.GridPlane(drawSize);

        //Set rotation
        if (!drawRotated)
        {
            extraRotation = 0;
            return;
        }

        var num = rot.AsAngle;
        num += g.DrawRotatedExtraAngleOffset;
        if ((rot == Rot4.West && g.WestFlipped) || (rot == Rot4.East && g.EastFlipped)) num += 180f;
        extraRotation = num;
    }

    //[TweakValue("FXLayer", 0, 10f)]
    //private static int CurrentLayer = 0;

    public void Draw(Vector3? drawLocOverride = null)
    {
        //
        var drawPos = drawLocOverride ?? DrawPos;
        GetDrawInfo(Graphic, parentInfo.ParentThing, parentInfo.ParentThing.def, ref drawPos, ParentRot4,
            parentInfo.Extension, out drawSize, out _drawMat, out drawMesh, out var extraRotation, out flipUV);

        //Colors
        var graphicColor = data.graphicData.color;
        if (ColorOverride != Color.white)
            graphicColor *= ColorOverride;

        graphicColor.a = Opacity;
        graphicColor *= drawColor;

        //
        _drawMat.SetTextureOffset("_MainTex", data.texCoords.position);
        _drawMat.SetTextureScale("_MainTex", data.texCoords.size);

        PropertyBlock.SetColor(ShaderPropertyIDs.Color, graphicColor);

        var rotationQuat = TrueRotation.ToQuat();

        if (data.PivotOffset != null)
        {
            var pivotPoint = drawPos + data.PivotOffset.Value;
            var relativePos = rotationQuat * (drawPos - pivotPoint);
            drawPos = pivotPoint + relativePos;
        }

        //
        if (DrawFunction != null)
        {
            var result = DrawFunction.Invoke(new RoutedDrawArgs
            {
                graphic = Graphic,
                drawPos = DrawPos,
                altitude = _altitude,
                rotation = TrueRotation,
                mesh = drawMesh
            });
            if (!result)
                return;
        }

        //
        Graphics.DrawMesh(drawMesh, new Vector3(drawPos.x, _altitude, drawPos.z), rotationQuat, _drawMat, 0, null, 0,
            PropertyBlock);
    }

    public void Print(SectionLayer layer)
    {
        var drawPos = DrawPos;
        GetDrawInfo(Graphic, parentInfo.ParentThing, parentInfo.ParentThing.def, ref drawPos, ParentRot4,
            parentInfo.Extension, out drawSize, out _drawMat, out drawMesh, out var extraRotation, out flipUV);
        Printer_Plane.PrintPlane(layer, new Vector3(drawPos.x, _altitude, drawPos.z), drawSize, _drawMat, TrueRotation,
            flipUV);
    }
}