using System;

namespace TeleCore
{
    public class DisposableAction : IDisposable
    {
        private Action action;

        public DisposableAction(Action action)
        {
            this.action = action;
        }

        public void DoAction()
        {
            action.Invoke();
            Dispose();
        }

        public void Dispose()
        {
            action = null;
        }
    }
}
