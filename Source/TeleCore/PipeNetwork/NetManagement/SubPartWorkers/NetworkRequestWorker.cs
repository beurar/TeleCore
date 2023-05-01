using System.Collections.Generic;
using System.Linq;
using TeleCore.FlowCore;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore;

/// <summary>
/// Manages the requesting process as a state machine
/// </summary>
public class NetworkRequestWorker : IExposable
{
    //Settings
    private readonly Dictionary<NetworkValueDef, (bool isActive, float desiredAmt)> _settings;
    private FloatRange _capactiyRange = new FloatRange(0.5f, 1f);
    private RequesterMode _mode = RequesterMode.Automatic;

    //Requester Stuff
    public INetworkRequester Requester { get; }

    /// <summary>
    /// This range defines the Requester behaviour.
    /// </summary>
    /// <param name="min">The minimum amount that may be reached before starting to request.</param>
    /// <param name="max">The maximum amount that can be requested.</param>
    public FloatRange ReqRange
    {
        get => _capactiyRange;
        private set => _capactiyRange = value; //new FloatRange(Mathf.Clamp01(value.min), Mathf.Clamp01(value.max));
    }

    public RequesterMode Mode => _mode;
    public Dictionary<NetworkValueDef, (bool isActive, float desiredAmt)> Settings => _settings;
    
    //State Machine
    public bool RequestingNow { get; private set; }
    public bool BelowMin => Requester.Container.StoredPercent < ReqRange.min;
    public bool AboveMax => Requester.Container.StoredPercent > ReqRange.max;

    public bool BelowMax => Requester.Container.StoredPercent < ReqRange.max;
    public bool AboveMin => Requester.Container.StoredPercent > ReqRange.min;
    
    public bool ShouldRequest
    {
        get
        {
            if (BelowMin) return true; //Start Requesting
            if (RequestingNow) return true; //Has not reached max
            return !AboveMax;
        }
    }

    public NetworkRequestWorker()
    {
    }
    
    public NetworkRequestWorker(INetworkRequester requester)
    {
        this.Requester = requester;

        _settings = new Dictionary<NetworkValueDef, (bool, float)>();
        var values = Requester.Props.AllowedValuesByRole[NetworkRole.Requester];
        foreach (var allowedValue in values)
        {
            _settings.Add(allowedValue, (true, 1.0f / values.Count));
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref _capactiyRange, nameof(_capactiyRange));
    }

    public void SetRange(FloatRange newRange)
    {
        ReqRange = newRange;
    }

    public void SetMode(RequesterMode mode)
    {
        this._mode = mode;
    }
    
    //
    public bool RequestTick()
    {
        //Process State Machine
        if (!RequestingNow && BelowMin) // Start requesting
        {
            RequestingNow = true;
        }
        if (RequestingNow && AboveMax) // Stop requesting
        {
            RequestingNow = false;
        }
        
        return ShouldRequest;
    }

    internal void DrawSettings(Rect rect)
    {
        Widgets.DrawWindowBackground(rect);

        var contentRect = rect.ContractedBy(5);
        Widgets.BeginGroup(contentRect);
        contentRect = contentRect.AtZero();

        var curX = 5;
        var allowedTypes = Requester.Props.AllowedValuesByRole[NetworkRole.Requester];
        foreach (var type in allowedTypes)
        {
            Rect typeRect = new Rect(curX, contentRect.height - 15, 10, 10);
            Rect typeSliderSetting = new Rect(curX, contentRect.height - (20 + 100), 10, 100);
            Rect typeFilterRect = new Rect(curX, typeSliderSetting.y - 10, 10, 10);
            Widgets.DrawBoxSolid(typeRect, type.valueColor);

            var previous = Settings[type];
            var previousValue = previous.Item2;
            var previousBool = previous.Item1;

            //
            var newValue = TWidgets.VerticalSlider(typeSliderSetting, previousValue, 0, 1f, 0.01f, Mode == RequesterMode.Manual);
            Widgets.Checkbox(typeFilterRect.position, ref previousBool, 10);
            TooltipHandler.TipRegion(typeSliderSetting, $"Value: {newValue}");

            Settings[type] = (previousBool, newValue);

            var totalRequested = Settings.Values.Sum(v => v.Item2);
            if (totalRequested > Requester.Container.Capacity)
            {
                if (previousValue < newValue)
                {
                    foreach (var type2 in allowedTypes)
                    {
                        if (type2 == type) continue;
                        var val = Settings[type2].Item2;
                        val = Mathf.Lerp(val, 0, 1f - newValue);
                        Settings[type2] = (Settings[type2].Item1, val);
                        //val = Mathf.Lerp(0, val, 1f - Mathf.InverseLerp(0, Container.Capacity, newValue));
                        //parentComp.RequestedTypes[type2] = Mathf.Clamp(parentComp.RequestedTypes[type2] - (diff / (parentComp.RequestedTypes.Count - 1)), 0, Container.Capacity);
                    }
                }
            }

            curX += 20 + 5;
        }

        Widgets.EndGroup();
    }
}