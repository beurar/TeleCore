using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public struct ActionPart
    {
        //
        private readonly int startTick = 0, endTick = 0, duration = 0;
        private int curTick = 0;

        //
        public Action<ActionPart> action = null;
        public SoundPart sound = SoundPart.Empty;

        public int StartTick => startTick;
        public int EndTick => endTick;
        public int Duration => duration;

        public int CurrentTick => curTick;
        public bool Instant => startTick == endTick;
        public bool Completed { get; set; } = false;

        public ActionPart()
        {
        }

        //Action Only
        public ActionPart(Action<ActionPart> action, float startSeconds, float duration = 0f)
        {
            this.action = action;
            this.startTick = startSeconds.SecondsToTicks();
            this.endTick = startTick + duration.SecondsToTicks();
            this.duration = duration.SecondsToTicks();
        }

        //Sound Only
        public ActionPart(SoundDef sound, SoundInfo info, float time, float duration = 0f)
        {
            this.sound = new SoundPart(sound, info);
            this.startTick = time.SecondsToTicks();
            this.endTick = startTick + duration.SecondsToTicks();
            this.duration = duration.SecondsToTicks();
        }

        //Action & Sounds
        public ActionPart(Action<ActionPart> action, SoundDef sound, SoundInfo info, float time, float duration = 0f)
        {
            this.action = action;
            this.sound = new SoundPart(sound, info);
            this.startTick = time.SecondsToTicks();
            this.endTick = startTick + duration.SecondsToTicks();
            this.duration = duration.SecondsToTicks();
        }

        public bool CanBeDoneNow(int compositionTick)
        {
            return startTick <= compositionTick && compositionTick <= endTick;
        }

        public void Tick(int compositionTick, int partIndex = 0)
        {
            if (Completed || !CanBeDoneNow(compositionTick)) return;
            //Play Sound Once - Always
            if (CurrentTick == 0)
            {
                sound.PlaySound();
            }

            action?.Invoke(this);
            TryComplete(compositionTick, partIndex);
            curTick++;
        }

        private bool TryComplete(int compositionTick, int index = 0)
        {
            if (Instant || compositionTick == endTick)
            {
                Completed = true;
                return true;
            }
            return false;
        }
    }
}
