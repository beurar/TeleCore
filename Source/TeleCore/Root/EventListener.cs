using System;
using UnityEngine;

namespace TeleCore;

public class EventListener
{
    private Action callback;
    private Func<Event, bool> listener;
    private string refID;

    public EventListener(Func<Event, bool> listener, Action callBack, string ID)
    {
        this.listener = listener;
        this.callback = callBack;
        this.refID = ID;
    }

    public string ID => refID;

    public void ListenToEvent(Event curEvent)
    {
        if (listener.Invoke(curEvent))
        {
            callback.Invoke();
        }
    }
}