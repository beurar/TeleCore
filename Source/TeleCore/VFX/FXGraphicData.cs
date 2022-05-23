using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class FXGraphicData
    {
        //Main graphic
        public GraphicData data;
        //Shader TextureParams
        public List<DynamicTextureParameter> textureParams;
        //
        //Fixed Alt Override
        public AltitudeLayer? altitude = null;
        public FXMode mode = FXMode.Static;

        //
        public bool skip = false;
        public bool needsPower = false;

        public int? drawLayer = null;

        //
        public RotateProperties rotate;
        public BlinkProperties blink;
        public FadeProperties fade;
        public ResizeProperties resize;

        //Texture Data
        public Rect texCoords = new Rect(0, 0, 1, 1);

        public Vector2 textureSize = Vector2.one;
        public Vector3 drawOffset = Vector3.zero;

        public Vector3? pivotOffset = null;
        public Vector3? pivotPixelOffset = null;

        public Vector3? PivotOffset
        {
            get
            {
                if (pivotOffset != null) return pivotOffset;
                if (pivotPixelOffset != null)
                {
                    var pixelOffset = pivotPixelOffset.Value;

                    float width = (pixelOffset.x / textureSize.x) * data.drawSize.x;
                    float height = (pixelOffset.z / textureSize.y) * data.drawSize.y;

                    pivotOffset = new Vector3(width, 0, height);
                }

                return pivotOffset;
            }
        }

        public Graphic Graphic => data.Graphic;
    }
}
