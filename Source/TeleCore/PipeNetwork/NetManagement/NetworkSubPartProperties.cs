using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TeleCore.Data.Network.IO;
using TeleCore.FlowCore;
using Verse;

namespace TeleCore;

public class NetworkSubPartProperties
{
    //Cached Data
    [Unsaved]
    private NetworkRole networkRole;
    [Unsaved]
    private Dictionary<NetworkRole, List<NetworkValueDef>> allowedValuesByRoleInt = null!;
    [Unsaved] 
    private NetworkCellIOSimple _simpleIO;
        
    //
    public bool requiresController;

    //Loaded from XML
    public Type workerType = typeof(NetworkSubPart);
    public NetworkDef networkDef;
    public ContainerConfig<NetworkValueDef> containerConfig;
    public List<NetworkRoleProperties> networkRoles = new(){ NetworkRole.Transmitter };
    public string subIOPattern;

    public NetworkCellIOSimple SimpleIO => _simpleIO;
        
    /// <summary>
    /// Provides sub-managed values by role, if set in the networkRole props.
    /// </summary>
    public Dictionary<NetworkRole, List<NetworkValueDef>> AllowedValuesByRole
    {
        get
        {
            if (allowedValuesByRoleInt == null)
            {
                allowedValuesByRoleInt = new Dictionary<NetworkRole, List<NetworkValueDef>>();
                foreach (var role in networkRoles)
                {
                    if (role.HasSubValues && role != NetworkRole.Transmitter)
                    {
                        allowedValuesByRoleInt.Add(role, role.subValues);
                        continue;
                    }
                    allowedValuesByRoleInt.Add(role, containerConfig.AllowedValues);
                }
            }
            return allowedValuesByRoleInt;
        }
    }

    public NetworkRole NetworkRole
    {
        get
        {
                
            if (networkRole == 0x0000)
            {
                networkRole = NetworkRole.Transmitter;
                foreach (var role in networkRoles)
                {
                    networkRole |= role;
                }
            }
            return networkRole;
        }
    }

    public void PostLoadSpecial(ThingDef parent)
    {
        if (subIOPattern == null) return;
        _simpleIO = new NetworkCellIOSimple(subIOPattern, parent);
    }
}