using System;
using System.Collections.Generic;
using System.Xml;
using TeleCore.FlowCore;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore.Network.Data;

public class NetworkPartConfig : Editable
{
    #region XML Fields

    public Type workerType = typeof(NetworkPart);
    public NetworkDef networkDef;
    public NetworkRole roles = NetworkRole.Transmitter;
    public bool requiresController;
    public NetIOConfig? netIOConfig;
    public FlowVolumeConfig<NetworkValueDef> volumeConfig;

    public override IEnumerable<string> ConfigErrors()
    {
        if(volumeConfig == null)
            yield return $"A network part cannot have a null volume config! (NetworkDef: {networkDef})";
    }

    public override void PostLoad()
    {
        volumeConfig?.PostLoad();
    }
    
    public void PostLoadSpecial(ThingDef parent)
    {
        netIOConfig?.PostLoadCustom(parent);
    }


    #endregion
}