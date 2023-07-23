using System.Collections.Generic;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore;

/// <summary>
/// </summary>
public class CompProperties_Network : CompProperties
{
    public NetIOConfig generalIOConfig = new NetIOConfig();
    public List<NetworkPartConfig> networks;

    public CompProperties_Network()
    {
        compClass = typeof(Comp_Network);
    }

    public override void PostLoadSpecial(ThingDef parent)
    {
        base.PostLoadSpecial(parent);
        generalIOConfig.PostLoadCustom(parent);
        foreach (var network in networks) 
            network.PostLoadSpecial(parent);
    }
}