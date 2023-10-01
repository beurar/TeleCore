using System;
using Verse;

namespace TeleCore.Events;

public class RegionStateChangedArgs : EventArgs
{
    public Map Map { get; set; }
    public IntVec3 Cell { get; set; }
    public Region Region { get; set; }
    public Room Room { get; set; }
}