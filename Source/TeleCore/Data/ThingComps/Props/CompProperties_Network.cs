using System.Collections.Generic;
using System.Text.RegularExpressions;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using TeleCore.Network.IO.Experimental;
using Verse;

namespace TeleCore;

/// <summary>
/// 
/// </summary>
public class CompProperties_Network : CompProperties
{
    [Unsaved] 
    private NetRenderIO _renderIO;
        
    //
    public List<NetworkPartConfig> networks;
    public NetIOConfig? generalIOConfig;

    public NetRenderIO RenderIO => _renderIO;
        
    public CompProperties_Network()
    {
        this.compClass = typeof(Comp_Network);
    }

    public override void PostLoadSpecial(ThingDef parent)
    {
        base.PostLoadSpecial(parent);

        foreach (var network in networks)
        {
            network.PostLoadSpecial(parent);
        }
            
        //
        if (generalIOConfig == null) return;
            
        _renderIO = new NetRenderIO(generalIOConfig.Value, parent);
    }
}