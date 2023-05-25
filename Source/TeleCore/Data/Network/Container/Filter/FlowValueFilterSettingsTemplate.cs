using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore.FlowCore;

public class FlowValueFilterSettingsTemplate<TValue> : IExposable where TValue: FlowValueDef
{
    protected Dictionary<TValue, FlowValueFilterSettings> filterSettings = new();
    protected bool canChange = true;

    public bool CanChange => canChange;
    
    public FlowValueFilterSettingsTemplate()
    {
    }

    public FlowValueFilterSettingsTemplate(List<TValue> acceptedTypes, FlowValueFilterSettings? defaultSettings = null)
    {
        canChange = defaultSettings == null;
        foreach (var value in acceptedTypes)
        {
            filterSettings.Add(value, defaultSettings ?? new FlowValueFilterSettings());
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref filterSettings, "typeFilter", LookMode.Def, LookMode.Deep);
    }

    #region Getters
    
    public bool CanReceive(TValue value)
    {
        return filterSettings.TryGetValue(value, out var settings) && settings.canReceive;
    }
    
    public bool CanStore(TValue value)
    {
        return filterSettings.TryGetValue(value, out var settings) && settings.canStore;
    }
    
    public bool CanTransfer(TValue value)
    {
        if (filterSettings.TryGetValue(value, out var settings))
        {
            return settings.canTransfer;
        }
        return false;
    }
    
    #endregion

    #region Setters

    public void SetSettings(TValue value, FlowValueFilterSettings filter)
    {    
        filterSettings[value] = filter;
    }
    
    public void SetCanReceive(TValue value, bool canReceive)
    {
        if (filterSettings.TryGetValue(value, out var settings))
        {
            settings.canReceive = canReceive;
            filterSettings[value] = settings;
        }
        else
        {
            filterSettings.Add(value, new FlowValueFilterSettings {canReceive = canReceive});
        }
    }
    
    public void SetCanStore(TValue value, bool canStore)
    {
        if (filterSettings.TryGetValue(value, out var settings))
        {
            settings.canStore = canStore;
            filterSettings[value] = settings;
        }
        else
        {
            filterSettings.Add(value, new FlowValueFilterSettings {canStore = canStore});
        }
    }
    
    public void SetCanTransfer(TValue value, bool canTransfer)
    {
        
        if (filterSettings.TryGetValue(value, out var settings))
        {
            settings.canTransfer = canTransfer;
            filterSettings[value] = settings;
        }
        else
        {
            filterSettings.Add(value, new FlowValueFilterSettings {canTransfer = canTransfer});
        }
    }

    #endregion

    public FlowValueFilterSettingsTemplate<TValue> Copy()
    {
        return new FlowValueFilterSettingsTemplate<TValue>()
        {
            filterSettings = filterSettings.Copy(),
            canChange = canChange
        };
    }

    public FlowValueFilterSettings SettingsFor(TValue value)
    {
        return filterSettings[value];
    }
}