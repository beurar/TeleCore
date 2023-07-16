using System;
using System.Collections.Generic;
using System.Xml;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore.Network.Data;

/// <summary>
///     <para>Allows to set a network value filter for a specific role.</para>
///     <para>For example: Storage role allows to contain type A, B, C</para>
///     <para>While Requester role only allows to request B, C</para>
/// </summary>
public class NetworkValueFilterByRole
{
    public NetworkRole role = NetworkRole.Transmitter;
    public List<NetworkValueDef> subValues;

    public NetworkValueFilterByRole()
    {
    }

    public NetworkValueFilterByRole(NetworkRole networkRole)
    {
        role = networkRole;
    }

    public bool HasSubValues => subValues != null;

    public static implicit operator NetworkRole(NetworkValueFilterByRole props)
    {
        return props.role;
    }

    public static implicit operator NetworkValueFilterByRole(NetworkRole role)
    {
        return new NetworkValueFilterByRole(role);
    }

    public bool IsRole(NetworkRole role)
    {
        return this == role;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        //
        if (xmlRoot.Name != "li")
        {
            TLog.Error("Definition for networkRole not a listing.");
            return;
        }

        if (xmlRoot.ChildNodes.Count == 1)
        {
            role = ParseHelper.FromString<NetworkRole>(xmlRoot.InnerText);
            return;
        }

        role = DirectXmlToObject.ObjectFromXml<NetworkRole>(xmlRoot.SelectSingleNode(nameof(role)), false);
        subValues = DirectXmlToObject.ObjectFromXml<List<NetworkValueDef>>(xmlRoot.SelectSingleNode(nameof(subValues)),
            true);
    }

    public override string ToString()
    {
        return role.ToString();
    }
}

public class FlowValueFilter
{
    public List<FlowValueDef> allowedValues;
}

public class NetworkValueFilter : FlowValueFilter
{
    public List<NetworkValueFilterByRole> allowedValuesByRole;

    [Unsaved] private Dictionary<NetworkRole, List<NetworkValueDef>>? allowedValuesByRoleInt;

    public List<NetworkValueDef> this[NetworkRole role] => allowedValuesByRoleInt[role];


    /// <summary>
    ///     Provides sub-managed values by role, if set in the networkRole props.
    /// </summary>
    public Dictionary<NetworkRole, List<NetworkValueDef>> AllowedValuesByRole
    {
        get
        {
            if (allowedValuesByRoleInt != null) return allowedValuesByRoleInt;

            allowedValuesByRoleInt = new Dictionary<NetworkRole, List<NetworkValueDef>>();
            foreach (var filter in allowedValuesByRole)
                if (filter.HasSubValues && filter != NetworkRole.Transmitter)
                    allowedValuesByRoleInt.Add(filter, filter.subValues);
            return allowedValuesByRoleInt;
        }
    }


    public void PostLoad()
    {
    }
}

public class FlowVolumeConfig<T> where T : FlowValueDef
{
    //private const int AREA_VALUE = 128;

    public List<T> allowedValues;

    public int capacity;
    public int area = 1;
    public int elevation = 0;
    public int height = 1;

    //We dont need this approach for now
    public double Volume => capacity;
    //public double Volume => area * height * AREA_VALUE;
}

public class NetworkPartConfig
{
    public void PostLoadSpecial(ThingDef parent)
    {
        netIOConfig?.PostLoad();
    }

    #region XML Fields

    public Type workerType = typeof(NetworkPart);
    public NetworkDef networkDef;
    public NetworkRole roles = NetworkRole.Transmitter;
    public NetworkValueFilter valueFilter;
    public bool requiresController;
    public NetIOConfig? netIOConfig;
    public FlowVolumeConfig<NetworkValueDef> volumeConfig;

    #endregion
}