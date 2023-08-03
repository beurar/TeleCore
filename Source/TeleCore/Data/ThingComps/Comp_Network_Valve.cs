using RimWorld;
using TeleCore.Data.Events;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore;

public class Comp_Network_Valve : Comp_Network
{
    private const float _valveOpenState = 0;
    private const float _valveClosedState = 66;
    private const int _ticksToTurn = 120;
    private const float radsPerTick = _valveClosedState / _ticksToTurn;
    private float _curState;

    private CompFlickable Flick { get; set; }
    protected override bool IsWorkingOverride => Flick.SwitchIsOn;

    public override Color? FX_GetColor(FXLayerArgs args)
    {
        if (args.layerTag == "Valve") return Color.white;
        return base.FX_GetColor(args);
    }

    public override float? FX_GetRotation(FXLayerArgs args)
    {
        if (args.layerTag == "Valve") return _curState;
        return null;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Flick = parent.TryGetComp<CompFlickable>();
    }

    internal override void TeleTick()
    {
        if (Flick.SwitchIsOn)
        {
            if (_curState > _valveOpenState)
                _curState -= radsPerTick;
        }
        else
        {
            if (_curState < _valveClosedState) _curState += radsPerTick;
        }
    }

    public override void Notify_SignalReceived(Signal signal)
    {
        base.Notify_SignalReceived(signal);
        if (signal.tag == KnownCompSignals.FlickedOn)
        {
            foreach (var part in NetworkParts)
            {
                part.Network.NetworkSystem.
            }    
        }

        if (signal.tag == KnownCompSignals.FlickedOff)
        {
            
        }
    }
}