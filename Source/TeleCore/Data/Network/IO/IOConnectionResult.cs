using Verse;

namespace TeleCore.Network.IO;

public struct IOConnectionResult
{
    public IntVec3 In { get; set; }
    public IntVec3 Out { get; set; }
    
    public NetworkIOMode InMode { get; set; }
    public NetworkIOMode OutMode { get; set; }
 
    public bool IsValid => In.IsValid && Out.IsValid && InMode != 0 && OutMode != 0;
    public bool IsBiDirectional => InMode == NetworkIOMode.TwoWay && OutMode == NetworkIOMode.TwoWay;
 
    public static implicit operator bool(IOConnectionResult result) => result.IsValid;
    
    public static IOConnectionResult Invalid => new IOConnectionResult()
    {
        In = IntVec3.Invalid,
        Out = IntVec3.Invalid,
        InMode = 0,
        OutMode = 0
    };
}