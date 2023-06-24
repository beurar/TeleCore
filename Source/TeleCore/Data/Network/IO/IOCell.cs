using TeleCore.Primitive;
using Verse;

namespace TeleCore.Network.IO;

public struct IOCell
{
    public IntVec3Rot pos;
    public NetworkIOMode mode;
    
   
    public IntVec3 Interface => pos.Pos + pos.Dir.Opposite.FacingCell; 
}

public struct IOCellPrototype
{
    public IntVec3 offset;
    public Rot4 direction;
    public NetworkIOMode mode;
}
