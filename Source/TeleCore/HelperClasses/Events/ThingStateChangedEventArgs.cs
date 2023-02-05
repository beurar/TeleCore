using System;
using Verse;

namespace TeleCore;

public enum ThingStateChangeType
{
    Spawned,
    Despawning,
    Despawned,
    StateChanged,
    SentSignal
}

public class ThingStateChangedEventArgs : EventArgs
{
    public ThingStateChangeType ChangeType { get; }
    public Thing Thing { get; }
    public string? CompSignal { get; }

    public ThingStateChangedEventArgs(ThingStateChangeType changeType, Thing thing, string compSignal = null)
    {
        ChangeType = changeType;
        Thing = thing;
        CompSignal = compSignal;
    }
}