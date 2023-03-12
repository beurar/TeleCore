using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore
{ 
    /// <summary>
    /// 
    /// </summary>
    public class TeleUpdateManager //: IExposable
    {
        private static TeleUpdateManager instance;

        //public OutsourceWorker OutsourceWorker;
        private readonly Queue<DisposableAction> mainThreadQueuedActions = new();

        private readonly List<TaggedAction> taggedTickAction;
        private readonly List<TaggedAction> taggedUpdateAction;
        private readonly List<TaggedAction> taggedOnGUIAction;
        
        //
        private Action tickActions;

        public enum TaggedActionType
        {
            Tick,
            Update,
            OnGUI
        }
        
        public TickManager BaseTickManager => Find.TickManager;
        public bool GameRunning => Current.Game != null && !Find.TickManager.Paused;

        public TeleUpdateManager()
        {
            instance = this;
            taggedTickAction = new List<TaggedAction>();
            taggedUpdateAction = new List<TaggedAction>();
            taggedOnGUIAction = new List<TaggedAction>();
        }

        public void Update()
        {
            if(taggedUpdateAction.Count <= 0)return;
            for (var i = taggedUpdateAction.Count - 1; i >= 0; i--)
            {
                if(i >= taggedUpdateAction.Count) continue;
                var action = taggedUpdateAction[i];
                action.DoAction();
            }
        }

        public void OnGUI()
        {
            if(taggedOnGUIAction.Count <= 0)return;
            for (var i = taggedOnGUIAction.Count - 1; i >= 0; i--)
            {
                if(i >= taggedOnGUIAction.Count) continue;
                var action = taggedOnGUIAction[i];
                action.DoAction();
            }
        }
        
        public void GameTick()
        {

        }

        public void Tick()
        {
            WorkSubscribedTickActions();
            WorkMainThreadActionQueue();
            
            if(taggedTickAction.Count <= 0)return;
            for (var i = taggedTickAction.Count - 1; i >= 0; i--)
            {
                if(i >= taggedTickAction.Count) continue;
                var action = taggedTickAction[i];
                action.DoAction();
            }
        }

        public void WorkSubscribedTickActions()
        {
            tickActions?.Invoke();
        }

        public void WorkMainThreadActionQueue()
        {
            if (mainThreadQueuedActions.Count <= 0) return;
            var next = mainThreadQueuedActions.Dequeue();
            next.DoAction();
        }
        
        public static void Notify_EnqueueNewSingleAction(Action action)
        {
            instance.mainThreadQueuedActions.Enqueue(new DisposableAction(action));
        }
        
        public static void Notify_AddNewTickAction(Action action)
        {
            TLog.Message("Added tick-action!");
            instance.tickActions += action;
        }

        public static void Notify_AddTaggedAction(TaggedActionType type, Action action, string tag)
        {
            switch (type)
            {
                case TaggedActionType.Tick:
                    instance.taggedTickAction.Add(new TaggedAction(action, tag));
                    break;
                case TaggedActionType.Update:
                    instance.taggedUpdateAction.Add(new TaggedAction(action, tag));
                    break;
                case TaggedActionType.OnGUI:
                    instance.taggedOnGUIAction.Add(new TaggedAction(action, tag));
                    break;
            }
        }
        
        public static void Remove_TaggedAction(TaggedActionType type, string tag)
        {
            switch (type)
            {
                case TaggedActionType.Tick:
                    instance.taggedTickAction.RemoveAll(t => t.Tag == tag);
                    break;
                case TaggedActionType.Update:
                    instance.taggedUpdateAction.RemoveAll(t => t.Tag == tag);
                    break;
                case TaggedActionType.OnGUI:
                    instance.taggedOnGUIAction.RemoveAll(t => t.Tag == tag);
                    break;
            }
        }
    }
}
