using System.Collections.Generic;
using TeleCore.FlowCore;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using TeleCore.Primitive;

namespace TeleCore.Network.Flow;

/// <summary>
///     The logical handler for fluid flow.
///     Area and height define the total content, elevation allows for flow control.
/// </summary>
public class NetworkVolume : FlowVolume<NetworkValueDef>
{
    public NetworkVolume() : base()
    {
    }
    
    public NetworkVolume(FlowVolumeConfig<NetworkValueDef> config) : base(config)
    {
    }
}