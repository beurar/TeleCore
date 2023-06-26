using System.Collections.Generic;
using System.Text.RegularExpressions;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore;

/// <summary>
/// 
/// </summary>
public class CompProperties_Network : CompProperties
{
    public List<NetworkPartConfig> networks;
    public NetIOConfig? generalIOConfig;

    public CompProperties_Network()
    {
        this.compClass = typeof(Comp_Network);
    }

    public override void PostLoadSpecial(ThingDef parent)
    {
        base.PostLoadSpecial(parent);
        generalIOConfig?.PostLoad(parent);
        foreach (var network in networks)
        {
            network.PostLoadSpecial(parent);
        }
    }
}