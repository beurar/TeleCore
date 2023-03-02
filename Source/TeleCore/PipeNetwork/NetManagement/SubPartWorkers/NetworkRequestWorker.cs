using System.Collections.Generic;
using System.ComponentModel;
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
    private FloatRange _capactiyRange = new FloatRange(0.5f, 0.5f);
    private RequesterMode _mode = RequesterMode.Automatic;

    //Requester Stuff
    public INetworkRequester Requester { get; }

    /// <summary>
    /// This range defines the Requester behaviour.
    /// </summary>
    /// <param name="min">The minimum amount that may be reached before starting to request.</param>
    /// <param name="max">The maximum amount that can be requested.</param>
    public FloatRange RequestedRange
    {
        get => _capactiyRange;
        private set => _capactiyRange = value; //new FloatRange(Mathf.Clamp01(value.min), Mathf.Clamp01(value.max));
    }

    public RequesterMode Mode => _mode;


    public Dictionary<NetworkValueDef, (bool isActive, float desiredAmt)> RequestedTypes => _settings;
    
    //State Machine
    public bool RequestingNow { get; private set; }
    public bool BelowMin => Requester.Container.StoredPercent < RequestedRange.min;
    public bool AboveMax => Requester.Container.StoredPercent > RequestedRange.max;

    public bool ShouldRequest
    {
        get
        {
            if (BelowMin) return true; //Start Requesting
            if (RequestingNow) return true; //Has not reached max
            return !AboveMax;
        }
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
        RequestedRange = newRange;
    }

    public void SetMode(RequesterMode mode)
    {
        this._mode = mode;
    }
    
    //
    public void RequestTick()
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


        //Actual Ticker
        if (ShouldRequest)
        {
            Request();
        }
    }

    private void Request()
    {
        var network = Requester.Network;
        var container = Requester.Container;
        
        //When set to automatic, adjusts settings to pull equal amounts of each value-type
        if (_mode == RequesterMode.Automatic)
        {
            //Resolve..
            //var maxVal = RequestedCapacityPercent * Container.Capacity;
            var maxPercent = RequestedRange.max; //Get max percentage
            foreach (var valType in Requester.Props.AllowedValuesByRole[NetworkRole.Requester])
            {
                //var requestedTypeValue = container.ValueForType(valType);
                var requestedTypeNetworkValue = network.TotalValueFor(valType, NetworkRole.Storage);
                if (requestedTypeNetworkValue > 0)
                {
                    var setValue = Mathf.Min(maxPercent, requestedTypeNetworkValue / Requester.Container.Capacity);
                    var tempVal = _settings[valType];

                    tempVal.Item2 = setValue;
                    _settings[valType] = tempVal;
                    maxPercent = Mathf.Clamp(maxPercent - setValue, 0, maxPercent);
                }
            }
        }

        //Pull values according to settings
        //if (container.StoredPercent >= RequestedCapacityPercent) return;
        foreach (var setting in _settings)
        {
            //If not requested, skip
            if (!setting.Value.isActive) continue;
            if (container.StoredPercentOf(setting.Key) < setting.Value.desiredAmt)
            {
                NetworkTransactionUtility.DoTransaction(new TransactionRequest(Requester.Part,
                    NetworkRole.Requester, NetworkRole.Storage,
                    part =>
                    {
                        var partContainer = part.Container;
                        if (partContainer.FillState == ContainerFillState.Empty) return;
                        if (partContainer.StoredValueOf(setting.Key) <= 0) return;
                        if (partContainer.TryTransferTo(container, setting.Key, 1, out _))
                        {
                            _ = true;
                            //Notify_ReceivedValue();
                        }
                    }));
            }
        }
    }
}