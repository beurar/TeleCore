using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public struct AnimationActionEventFlag
    {
        public string eventTag;
        public int frameTick;
        
        public string EventTag => eventTag;
        public int Frame => frameTick;
        
        public AnimationActionEventFlag(int currentFrame, string flag)
        {
            frameTick = currentFrame;
            eventTag = flag;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref frameTick, nameof(frameTick), 0, true);
            Scribe_Deep.Look(ref eventTag, nameof(eventTag));
        }
    }
}
