using System;
using TeleCore.Primitive;

namespace TeleCore.FlowCore.Events;

public class FlowEventArgs : EventArgs
{
    public FlowEventArgs(DefValue<NetworkValueDef, double> valueChange)
    {
        Value = valueChange;
    }

    public DefValue<NetworkValueDef, double> Value { get; private set; }
}