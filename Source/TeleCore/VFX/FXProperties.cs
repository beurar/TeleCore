using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TeleCore
{
    public enum FXPropertyEnum
    {
        Opacity
    }

    /// <summary>
    /// Experimental cache of property values, to reduce recreation of the array on each frame.
    /// </summary>
    public struct FXProperties
    {
        private float[] OpacityFloats;
        private float?[] RotationOverrides;
        private float?[] AnimationSpeeds;

        private bool[] DrawBools;

        private Color[] ColorOverrides;

        private Vector3[] DrawPositions;

        private Vector2? TextureOffset;
        private Vector2? TextureScale;

        private Action<FXGraphic>[] Actions;
        private bool ShouldDoEffecters;

        public FXProperties(int size)
        {
            OpacityFloats = new float[size];
            RotationOverrides = new float?[size];
            AnimationSpeeds = new float?[size];
            DrawBools = new bool[size];
            ColorOverrides = new Color[size];
            DrawPositions = new Vector3[size];
            TextureOffset = Vector2.zero;
            TextureScale = Vector2.one;
            Actions = new Action<FXGraphic>[size];
            ShouldDoEffecters = false;
        }

        public void SetEffecters(bool effecterState)
        {
            ShouldDoEffecters = effecterState;
        }

        public void SetValue(FXPropertyEnum type, int index, object value)
        {
            switch (type)
            {

            }
        }
    }
}
