using System;

namespace TeleCore.Events;

public enum RoomChangeType
{
    Created,
    Disbanded,
    Reused
}

public class RoomChangedArgs : EventArgs
{
    public RoomChangeType ChangeType { get; }
    public RoomTracker RoomTracker { get; }
    
    public RoomChangedArgs(RoomChangeType created, RoomTracker actionTracker)
    {
        ChangeType = created;
        RoomTracker = actionTracker;
    }
}