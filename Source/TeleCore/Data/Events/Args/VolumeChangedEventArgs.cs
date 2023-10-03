using System;
using TeleCore.FlowCore;

namespace TeleCore.Events;

public class VolumeChangedEventArgs<TValue> : EventArgs
where TValue : FlowValueDef
{
    public ChangedAction Action { get; }
    public FlowVolumeBase<TValue> Volume { get; }
    
    public enum ChangedAction
    {
        Invalid,
        AddedValue,
        RemovedValue,
        Filled,
        Emptied
    }

    public VolumeChangedEventArgs(ChangedAction action, FlowVolumeBase<TValue> volume)
    {
        Action = action;
        Volume = volume;
    }
}