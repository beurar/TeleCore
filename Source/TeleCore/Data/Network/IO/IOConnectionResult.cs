using Verse;

namespace TeleCore.Data.Network.IO;

public struct IOConnectionResult
{
    public static implicit operator bool(IOConnectionResult result) => result.IsValid;
    
    public IntVec3 In { get; set; }
    public IntVec3 Out { get; set; }
            
    public NetworkIOMode InMode { get; set; }
    public NetworkIOMode OutMode { get; set; }
 
    public bool IsValid => In.IsValid && Out.IsValid && InMode != NetworkIOMode.None && OutMode != NetworkIOMode.None;
    public bool IsBiDirectional => InMode == NetworkIOMode.TwoWay && OutMode == NetworkIOMode.TwoWay;
    
    public static IOConnectionResult Invalid => new IOConnectionResult()
    {
        In = IntVec3.Invalid,
        Out = IntVec3.Invalid,
        InMode = NetworkIOMode.None,
        OutMode = NetworkIOMode.None
    };
}