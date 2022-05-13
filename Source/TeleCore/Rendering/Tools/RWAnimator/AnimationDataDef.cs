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
    public class AnimationDataDef : Def, IExposable
    {
        //
        public List<AnimationSet> animationSets;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Collections.Look(ref animationSets, "animationSets", LookMode.Deep);
        }
    }

    public struct AnimationSet : IExposable
    {
        //Layers
        public List<TextureData> textureParts;
        public List<AnimationPart> animations;

        public bool HasTextures => textureParts != null;
        public bool HasAnimations => animations != null;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref textureParts, "textureParts", LookMode.Deep);
            Scribe_Collections.Look(ref animations, "animations", LookMode.Deep);
        }
    }

    public struct AnimationPart : IExposable
    {
        public string tag;
        public int frames;
        public List<ScribeList<KeyFrame>> keyFrames;
        public IntRange bounds;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tag, "tag");
            Scribe_Values.Look(ref frames, "frames",0, true);
            Scribe_Values.Look(ref bounds, "bounds", new IntRange(0, frames), true);
            Scribe_Collections.Look(ref keyFrames, "keyFrames", LookMode.Deep);
        }
    }
}
