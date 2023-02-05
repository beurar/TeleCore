using System;

namespace TeleCore;

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
    public INetworkSubPart Part { get; }

    public NetworkChangedEventArgs(NetworkChangeType changeType)
    {
        ChangeType = changeType;
        Part = null;
    }
    
    public NetworkChangedEventArgs(NetworkChangeType changeType, INetworkSubPart part)
    {
        ChangeType = changeType;
        Part = part;
    }
}