using System.Collections.Generic;
using System.Linq;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using TeleCore.Network.IO;
using TeleCore.Static;
using Verse;

namespace TeleCore.Network.Utility;

public static class PipeNetworkFactory
{
    internal static int MasterNetworkID = 0;
    
    /// <summary>
    /// Checks whether or not a thing is part of a specific network.
    /// </summary>
    internal static bool Fits(Thing thing, NetworkDef network, out INetworkPart part)
    {
        part = null;
        if (thing is not ThingWithComps compThing)
            return false;

        var networkComp = compThing.GetComp<Comp_Network>();
        if (networkComp == null) return false;

        part = networkComp[network];
        return part != null;
    }
}