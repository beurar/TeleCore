using System;
using Verse;

namespace TeleCore.Data.Events;

public class ThingStateChangedEventArgs : EventArgs
{
    public ThingChangeFlag ChangeMode { get; }
    public Thing Thing { get; }
    public string? CompSignal { get; }

    public ThingStateChangedEventArgs(ThingChangeFlag changeMode, Thing thing, string compSignal = null)
    {
        ChangeMode = changeMode;
        Thing = thing;
        CompSignal = compSignal;
    }
}