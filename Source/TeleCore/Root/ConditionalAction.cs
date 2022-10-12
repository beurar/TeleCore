using System;

namespace TeleCore;

public class ConditionalAction: IDisposable
{
    private Action action;
    private Func<bool> stopCondition;

    public bool ShouldDispose => stopCondition.Invoke();

    public ConditionalAction(Action action, Func<bool> stopCondition)
    {
        this.action = action;
        this.stopCondition = stopCondition;
    }
    
    public void DoAction()
    {
        action?.Invoke();
    }

    public void Dispose()
    {
        action = null;
    }
}