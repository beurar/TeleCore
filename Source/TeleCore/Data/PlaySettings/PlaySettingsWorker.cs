using UnityEngine;

namespace TeleCore;

/// <summary>
/// Provides an entry point to add a custom PlaySettings Option
/// </summary>
public abstract class PlaySettingsWorker
{
    public abstract bool Visible { get; }
    public bool Active { get; internal set; }

    public virtual bool ShowOnWorldView { get; } = false;
    public virtual bool ShowOnMapView { get; } = true;

    public virtual bool DefaultValue { get; } = false;
    
    public abstract Texture2D Icon { get; }

    public abstract string Description { get; }
    
    public abstract void OnToggled(bool isActive);

    protected PlaySettingsWorker()
    {
        Active = DefaultValue;
    }

    internal void Toggle()
    {
        Active = !Active;
        OnToggled(Active);
    }
}