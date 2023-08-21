using Verse;

namespace TeleCore.Network.IO;

public struct IOConnectionResult
{
    public static implicit operator bool(IOConnectionResult result)
    {
        return result.IsValid;
    }

    /// <summary>
    /// The position on the OTHER network part that this part connected to.
    /// </summary>
    public IntVec3 SelfConnPos { get; set; }
    
    /// <summary>
    /// The position on THIS network part that the other part connected to.
    /// </summary>
    public IntVec3 OtherConnPos { get; set; }

    public NetworkIOMode InMode { get; set; }
    public NetworkIOMode OutMode { get; set; }

    public bool IsValid => SelfConnPos.IsValid && OtherConnPos.IsValid && InMode != NetworkIOMode.None && OutMode != NetworkIOMode.None;
    public bool IsBiDirectional => InMode == NetworkIOMode.TwoWay && OutMode == NetworkIOMode.TwoWay;

    public static IOConnectionResult Invalid => new()
    {
        SelfConnPos = IntVec3.Invalid,
        OtherConnPos = IntVec3.Invalid,
        InMode = NetworkIOMode.None,
        OutMode = NetworkIOMode.None
    };
}