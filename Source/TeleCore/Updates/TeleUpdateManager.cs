using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{ 
    /// <summary>
    /// 
    /// </summary>
    public class TeleUpdateManager
    {
        private static TeleUpdateManager instance;

        //public OutsourceWorker OutsourceWorker;
        private Queue<DisposableAction> mainThreadQueuedActions = new Queue<DisposableAction>();
        private Action tickActions;

        public TickManager BaseTickManager => Find.TickManager;
        public bool GameRunning => Current.Game != null && !Find.TickManager.Paused;

        public TeleUpdateManager()
        {
            instance = this;
        }

        public void Update()
        {

        }

        public void GameTick()
        {

        }

        public void Tick()
        {
            WorkSubscribedTickActions();
            WorkMainThreadActionQueue();
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

        public static void Notify_AddNewTickAction(Action action)
        {
            TLog.Message("Added tick-action!");
            instance.tickActions += action;
        }

        public static void Notify_EnqueueNewSingleAction(Action action)
        {
            instance.mainThreadQueuedActions.Enqueue(new DisposableAction(action));
        }
    }
}
