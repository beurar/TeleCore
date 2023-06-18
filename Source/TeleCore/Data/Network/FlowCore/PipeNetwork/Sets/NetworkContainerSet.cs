using System.Collections.Generic;
using TeleCore.FlowCore;
using TeleCore.Network;
using UnityEngine;
using Verse;

namespace TeleCore;

/// <summary>
/// 
/// </summary>
public class NetworkContainerSet
{
    private Color networkColor;
    private float totalNetworkValue;

    //Type Cache
    private readonly HashSet<NetworkValueDef> allStoredTypes;

    //Containers
    private readonly HashSet<NetworkContainer> allContainers;
    private readonly Dictionary<NetworkRole, HashSet<NetworkContainer>> containersByRole;

    //Values
    private readonly Dictionary<NetworkRole, float> totalValueByRole;
    private readonly Dictionary<NetworkValueDef, float> totalValueByType;
    private readonly Dictionary<NetworkRole, Dictionary<NetworkValueDef, float>> valueByTypeByRole;

    public float TotalNetworkValue => totalNetworkValue;
    public float TotalStorageValue => GetTotalValueByRole(NetworkRole.Storage);
    public IEnumerable<NetworkValueDef> AllTypes => allStoredTypes;

    public HashSet<NetworkContainer> this[NetworkRole role]
    {
        get
        {
            if (role == NetworkRole.AllFlag)
                return allContainers;

            var set = new HashSet<NetworkContainer>();
            foreach (var @enum in role.AllFlags())
            {
                if (containersByRole.TryGetValue(@enum, out var valueSet))
                    set.AddRange(valueSet);
            }

            return set;
        }
    }

    public NetworkContainerSet()
    {
        //
        allStoredTypes = new();
        allContainers = new();
        containersByRole = new();
        totalValueByRole = new();
        totalValueByType = new();
        valueByTypeByRole = new();
    }

    //
    public float GetValueByType(NetworkValueDef def)
    {
        return totalValueByType.GetValueOrDefault(def, 0);
    }

    public float GetTotalValueByRole(NetworkRole role)
    {
        float totalVal = 0;
        foreach (var @enum in role.AllFlags())
        {
            totalVal += totalValueByRole.GetValueOrDefault(@enum, 0);
        }
        return totalVal;
    }

    public float GetValueByTypeByRole(NetworkValueDef type, NetworkRole inRole)
    {
        float totalVal = 0;
        foreach (var role in inRole.AllFlags())
            totalVal += valueByTypeByRole.GetValueOrDefault(role, null)?.GetValueOrDefault(type, 0) ?? 0;

        return totalVal;
    }

    //Value Data
    public void Notify_AddedValue(NetworkValueDef type, float value, INetworkSubPart part)
    {
        //Increment total value
        totalNetworkValue += value;

        //Increment by type
        if (!totalValueByType.TryAdd(type, value))
            totalValueByType[type] += value;

        foreach (var enums in part.NetworkRole.AllFlags())
        {
            //Increment by role
            if (!totalValueByRole.TryAdd(enums, value))
                totalValueByRole[enums] += value;
            //Increment by type by role
            if (!valueByTypeByRole.TryAdd(enums, new Dictionary<NetworkValueDef, float>() { { type, value } }))
                if (!valueByTypeByRole[enums].TryAdd(type, value))
                    valueByTypeByRole[enums][type] += value;
        }

        //Add type to known types
        allStoredTypes.Add(type);
    }

    public void Notify_RemovedValue(NetworkValueDef type, float value, INetworkSubPart part)
    {
        totalNetworkValue -= value;

        //
        if (!totalValueByType.ContainsKey(type))
        {
            TLog.Warning($"Tried to remove ({value}){type} in ContainerSet but {type} is not stored!");
        }
        else
        {
            totalValueByType[type] -= value;
            //Remove type if empty
            if (totalValueByType[type] <= 0)
                allStoredTypes.Remove(type);
        }

        //
        foreach (var enums in part.NetworkRole.AllFlags())
        {
            if (!totalValueByRole.ContainsKey(enums))
            {
                TLog.Warning($"Tried to remove ({value}){type} for role {enums} in ContainerSet but {enums} is not stored!");
            }
            else
            {
                totalValueByRole[enums] -= value;
                //if(TotalValueByRole[comp.NetworkRole] <= 0)
            }

            if (valueByTypeByRole.ContainsKey(enums))
            {
                if (valueByTypeByRole[enums].ContainsKey(type))
                {
                    valueByTypeByRole[enums][type] -= value;
                }
            }
        }
    }

    //Container Data
    public bool AddNewContainerFrom(INetworkSubPart part)
    {
        if (!part.HasContainer) return false;

        //
        if (allContainers.Add(part.Container))
        {
            foreach (var @enum in part.NetworkRole.AllFlags())
            {
                if (!containersByRole.ContainsKey(@enum))
                    containersByRole.Add(@enum, new());
                containersByRole[@enum].Add(part.Container);
            }

            //Adjust values
            foreach (var values in part.Container.StoredValuesByType)
            {
                Notify_AddedValue(values.Key, values.Value, part);
            }
            return true;
        }
        return false;
    }

    public void RemoveContainerFrom(INetworkSubPart part)
    {
        if (allContainers.Remove(part.Container))
        {
            foreach (var @enum in part.NetworkRole.AllFlags())
            {
                this[@enum].Remove(part.Container);
            }

            //Adjust values
            foreach (var values in part.Container.StoredValuesByType)
            {
                Notify_RemovedValue(values.Key, values.Value, part);
            }
        }
    }
}