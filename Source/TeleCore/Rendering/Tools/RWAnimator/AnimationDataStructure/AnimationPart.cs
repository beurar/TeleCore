using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public struct AnimationPart : IExposable
    {
        public string tag;
        public int frames;
        public List<ScribeList<KeyFrame>> keyFrames;
        public List<AnimationActionEventFlag> eventFlags;
        public IntRange bounds;

        //Loading
        public void ExposeData()
        {
            Scribe_Values.Look(ref tag, nameof(tag));
            Scribe_Values.Look(ref frames, nameof(frames), 0, true);
            Scribe_Values.Look(ref bounds, nameof(bounds), new IntRange(0, frames), true);
            Scribe_Collections.Look(ref keyFrames, nameof(keyFrames), LookMode.Deep);
            Scribe_Collections.Look(ref eventFlags, nameof(eventFlags), LookMode.Deep);
        }
    }
}
