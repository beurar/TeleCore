using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public class ActionComposition
    {
        private List<ActionPart> actionParts = new ();
        private Action finalAction;
        private int curTick, startTick, endTick;
        public GlobalTargetInfo target;

        private readonly string compositionName;

        public int CurrentTick => curTick;
        public int ActionCount => actionParts.Count;

        public ActionComposition(string name)
        {
            compositionName = name;

        }

        public void CacheMap(GlobalTargetInfo target)
        {
            this.target = target;
        }

        public void AddFinishAction(Action action)
        {
            finalAction = action;
        }

        public void AddPart(Action<ActionPart> action, float atSecond, float duration = 0)
        {
            actionParts.Add(new ActionPart(action, atSecond, duration));
            //TLog.Debug("[" + compositionName + "]Adding Action Part at " + time + " for " + playTime + "s");
        }

        public void AddPart(Action<ActionPart> action, SoundDef sound, SoundInfo info, float atSecond, float duration = 0)
        {
            actionParts.Add(new ActionPart(action, sound, info, atSecond, duration));
            //TLog.Debug("[" + compositionName + "]Adding Action/Sound Part at " + time + " for " + playTime + "s");
        }

        public void AddPart(SoundDef sound, SoundInfo info, float atSecond, float duration = 0)
        {
            actionParts.Add(new ActionPart(null, sound, info, atSecond, duration));
            //TLog.Debug("[" + compositionName + "]Adding Sound Part at " + time + " for " + playTime + "s");
        }

        public void Init()
        {
            startTick = actionParts.First().StartTick;
            endTick = actionParts.Last().EndTick;
            TLog.Message($"Init ActionComposition: {startTick} -> {endTick}");
            ActionCompositionHandler.InitComposition(this);
        }

        public void FinalizeComposition()
        {
            finalAction?.Invoke();
            ActionCompositionHandler.RemoveComposition(this);
        }

        public void Tick()
        {
            if (actionParts.All(a => a.Completed))
            {
                FinalizeComposition();
                return;
            }

            if (curTick >= endTick)
            {
                TLog.Warning("Force completing...");
                FinalizeComposition();
                return;
            }

            for (var i = 0; i < actionParts.Count; i++)
            {
                var part = actionParts[i];
                ActionPart.Tick(ref part, curTick, i);
                actionParts[i] = part;
            }
            curTick++;
        }

        public override string ToString()
        {
            return $"ActionComp '{compositionName}'";
        }
    }
}
