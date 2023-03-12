using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Verse;

namespace TeleCore
{
    public static class GPUTools
    {
        private static FilterMode defaultFilterMode = FilterMode.Bilinear;
        private static GraphicsFormat defaultGraphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;

        private static void MakeIonBubble(IntVec3 pos, float time, float bubbleRadius, Color color, ThingDef ionDistortion)
        {
            ActionComposition composition = new ActionComposition("Ion Bübble " + ionDistortion);
            Mote mote = (Mote)ThingMaker.MakeThing(ThingDef.Named("IonBubble"));
            Mote distortion = (Mote)ThingMaker.MakeThing(ionDistortion);
            composition.AddPart(delegate
            {
                mote.exactPosition = distortion.exactPosition = pos.ToVector3Shifted();
                mote.Scale = bubbleRadius;
                mote.rotationRate = distortion.rotationRate = 1.2f;
                mote.instanceColor = color;
                GenSpawn.Spawn(mote, pos, Find.CurrentMap);
                GenSpawn.Spawn(distortion, pos, Find.CurrentMap);
            }, 0);
            composition.AddPart(delegate (ActionPart part)
            {
                distortion.Scale = mote.Scale = bubbleRadius * (part.CurrentTick / (float)part.Duration);
            }, 0, time);
            composition.Init();
        }

        public static void CreateRenderTexture(ref RenderTexture texture, int width, int height)
        {
            CreateRenderTexture(ref texture, width, height, defaultFilterMode);
        }

        public static void CreateRenderTexture(ref RenderTexture texture, int width, int height, FilterMode filterMode)
        {
            if (texture == null || !texture.IsCreated() || texture.width != width || texture.height != height)
            {
                if (texture != null)
                {
                    texture.Release();
                }
                texture = new RenderTexture(width, height, 0);
                //texture.graphicsFormat = format;
                texture.enableRandomWrite = true;

                texture.autoGenerateMips = false;
                texture.Create();
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = filterMode;
        }

        /// Copy the contents of one render texture into another. Assumes textures are the same size.
        public static void CopyRenderTexture(Texture source, RenderTexture target)
        {
            Graphics.Blit(source, target);
        }
    }
}
