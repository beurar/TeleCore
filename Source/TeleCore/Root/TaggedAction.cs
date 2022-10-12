using System;
using UnityEngine;

namespace TeleCore;

public class TaggedAction
{
    private Action callback;
    private string tag;

    public TaggedAction(Action callback, string tag)
    {
        this.callback = callback;
        this.tag = tag;
    }

    public string Tag => tag;

    public void DoAction()
    {
        callback.Invoke();
    }
}