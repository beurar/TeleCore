using System;
using TeleCore.Network.Data;

namespace TeleCore.Events;

public enum NetworkChangeType
{
    Created,
    Destroyed,
    AddedPart,
    RemovedPart,
}

public class NetworkChangedEventArgs : EventArgs
{
    public NetworkChangeType ChangeType { get; }
    public INetworkPart Part { get; }

    public NetworkChangedEventArgs(NetworkChangeType changeType)
    {
        ChangeType = changeType;
        Part = null;
    }
    
    public NetworkChangedEventArgs(NetworkChangeType changeType, INetworkPart part)
    {
        ChangeType = changeType;
        Part = part;
    }
}