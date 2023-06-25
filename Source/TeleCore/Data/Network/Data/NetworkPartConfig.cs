using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using TeleCore.Defs;
using TeleCore.Network.IO.Experimental;
using Verse;
using NetworkIOMode = TeleCore.Network.IO.NetworkIOMode;

namespace TeleCore.Network.Data;

/// <summary>
/// <para>Allows to set a network value filter for a specific role.</para>
/// <para>For example: Storage role allows to contain type A, B, C</para>
/// <para>While Requester role only allows to request B, C</para>
/// </summary>
public class NetworkValueFilterByRole
{
    public NetworkRole role = NetworkRole.Transmitter;
    public List<NetworkValueDef> subValues;

    public bool HasSubValues => subValues != null;

    public NetworkValueFilterByRole(){}

    public NetworkValueFilterByRole(NetworkRole networkRole)
    {
        this.role = networkRole;
    }

    public static implicit operator NetworkRole(NetworkValueFilterByRole props) => props.role;
    public static implicit operator NetworkValueFilterByRole(NetworkRole role) => new NetworkValueFilterByRole(role);

    public bool IsRole(NetworkRole role)
    {
        return this == role;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        //
        if (xmlRoot.Name != "li")
        {
            TLog.Error($"Definition for networkRole not a listing.");
            return;
        }

        if (xmlRoot.ChildNodes.Count == 1)
        {
            role = ParseHelper.FromString<NetworkRole>(xmlRoot.InnerText);
            return;
        }

        role = DirectXmlToObject.ObjectFromXml<NetworkRole>(xmlRoot.SelectSingleNode(nameof(role)), false);
        subValues = DirectXmlToObject.ObjectFromXml<List<NetworkValueDef>>(xmlRoot.SelectSingleNode(nameof(subValues)), true);
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
    [Unsaved]
    private Dictionary<NetworkRole, List<NetworkValueDef>>? allowedValuesByRoleInt = null;
    
    public List<NetworkValueFilterByRole> allowedValuesByRole;

    public List<NetworkValueDef> this[NetworkRole role] => allowedValuesByRoleInt[role];
    
    
    /// <summary>
    /// Provides sub-managed values by role, if set in the networkRole props.
    /// </summary>
    public Dictionary<NetworkRole, List<NetworkValueDef>> AllowedValuesByRole
    {
        get
        {
            if (allowedValuesByRoleInt != null) return allowedValuesByRoleInt;
            
            allowedValuesByRoleInt = new Dictionary<NetworkRole, List<NetworkValueDef>>();
            foreach (var filter in allowedValuesByRole)
            {
                if (filter.HasSubValues && filter != NetworkRole.Transmitter)
                {
                    allowedValuesByRoleInt.Add(filter, filter.subValues);
                }
            }
            return allowedValuesByRoleInt;
        }
    }
    

    public void PostLoad()
    {
        
    }
}

public class NetworkPartConfig
{
    #region Cached Data
    
    [Unsaved]
    private NetworkRole networkRole;

    #endregion

    #region XML Fields
    
    public Type workerType = typeof(NetworkPart);
    public NetworkDef networkDef;
    public NetworkRole role = NetworkRole.Transmitter;
    public NetworkValueFilter valueFilter;
    public bool requiresController;
    public NetIOConfig netIOConfig;

    #endregion
    

    public void PostLoadSpecial(ThingDef parent)
    {
        //TODO:
        //if (subIOPattern == null) return;
        //_simpleIO = new NetworkCellIOSimple(subIOPattern, parent);
    }

    internal void DrawForPlaceworker(IntVec3 center, ThingDef def, Rot4 rot)
    {

        foreach (var renderIOCell in _cells)
        {
            var cell = center + renderIOCell.pos;
            var drawPos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);

            switch (renderIOCell.mode)
            {
                case NetworkIOMode.Input:
                    Graphics.DrawMesh(MeshPool.plane10, drawPos,
                        (renderIOCell.pos.Direction.AsAngle - 180).ToQuat(), TeleContent.IOArrow, 0);
                    break;
                case NetworkIOMode.Output:
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, renderIOCell.pos.Direction.AsQuat,
                        TeleContent.IOArrow, 0);
                    break;
                case NetworkIOMode.TwoWay:
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, renderIOCell.pos.Direction.AsQuat,
                        TeleContent.IOArrowTwoWay, 0);
                    break;
            }
        }

    }

}