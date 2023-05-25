namespace TeleCore.Network.IO;

public struct RenderIOCell
{
    public NetworkIOMode mode;
    public IntVec3Rot pos;
        
    public RenderIOCell(IntVec3Rot pos, NetworkIOMode mode)
    {
        this.pos = pos;
        this.mode = mode;
    }
}