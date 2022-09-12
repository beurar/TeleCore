using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace TeleCore
{

    public class ExposableAction<TDelegate> : IExposable where TDelegate : Delegate
    {
        private object _Target;
        private TDelegate _Delegate;

        public static implicit operator ExposableAction<TDelegate>(TDelegate del) => new()
        {
            _Delegate = del,
            _Target = del.Target
        };

        public static explicit operator TDelegate(ExposableAction<TDelegate> e) => e._Delegate;

        public void ExposeData()
        {
        }
    }

    public class ActionPart : IExposable
    {
        //
        private int startTick = 0, endTick = 0, duration = 0;
        private int curTick = 0;
        private bool completed = false;

        //
        public Action<ActionPart> action = null;
        public SoundPart sound = SoundPart.Empty;

        public int StartTick => startTick;
        public int EndTick => endTick;
        public int Duration => duration;

        public int CurrentTick => curTick;
        public bool Instant => startTick == endTick;

        public bool Completed
        {
            get => completed;
            private set => completed = value;
        }

        public ActionPart()
        {

            //Sound

        }

        private ScribeDelegate<Action<ActionPart>> _ScribedDelegate;

        public void ExposeData()
        {
            //Ticks
            Scribe_Values.Look(ref curTick, "curTick");
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref endTick, "endTick");
            Scribe_Values.Look(ref duration, "duration");
            Scribe_Values.Look(ref completed, "completed");

            //Method
            if(Scribe.mode == LoadSaveMode.Saving)
                _ScribedDelegate = new ScribeDelegate<Action<ActionPart>>(action);
            
            Scribe_Deep.Look(ref _ScribedDelegate, "partAction");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                action = _ScribedDelegate.@delegate;
            }
        }

        //Action Only
        public ActionPart(Action<ActionPart> action, float startSeconds, float duration = 0f)
        {
            this.action = action;
            this.startTick = startSeconds.SecondsToTicks();
            this.endTick = startTick + duration.SecondsToTicks();
            this.duration = duration.SecondsToTicks();

            if(action.Target is not ILoadReferenceable referencable)
                TLog.Error("Delegate target not the same as scribable reference.");
            
            

            TLog.Message($"Making New ActionPart: {startTick} -> {endTick} | {this.duration}");
        }

        //Sound Only
        public ActionPart(SoundDef sound, SoundInfo info, float time, float duration = 0f)
        {
            this.sound = new SoundPart(sound, info);
            this.startTick = time.SecondsToTicks();
            this.endTick = startTick + duration.SecondsToTicks();
            this.duration = duration.SecondsToTicks();

            TLog.Message($"Making New ActionPart: {startTick} -> {endTick} | {this.duration}");
        }

        //Action & Sounds
        public ActionPart(Action<ActionPart> action, SoundDef sound, SoundInfo info, float time, float duration = 0f)
        {
            this.action = action;
            this.sound = new SoundPart(sound, info);
            this.startTick = time.SecondsToTicks();
            this.endTick = startTick + duration.SecondsToTicks();
            this.duration = duration.SecondsToTicks();

            TLog.Message($"Making New ActionPart: {startTick} -> {endTick} | {this.duration}");
        }

        public bool CanBeDoneNow(int compositionTick)
        {
            return startTick <= compositionTick && (compositionTick <= endTick);
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
                TLog.Message($"Finishing actionpart[{startTick}]");
                return true;
            }
            return false;
        }
    }
}
