using UnityEngine;
using Verse;

namespace TeleCore.Static.Utilities;

public class ContainerTransferUtility
{
    public const float MIN_EQ_VAL = 2;

    public static bool NeedsEqualizing<T>(BaseContainer<T> containerA, BaseContainer<T> containerB, float minDiffMargin, out ValueFlowDirection flow, out float diffPct) where T : FlowValueDef
    {
        flow = ValueFlowDirection.None;
        diffPct = 0f;
        
        var fromTotal = containerA.TotalStored;
        var toTotal = containerB.TotalStored;

        var fromPct = containerA.StoredPercent;
        var toPct = containerB.StoredPercent;
        
        var totalDiff = Mathf.Abs(fromTotal - toTotal);
        diffPct = fromPct - toPct;

        flow = diffPct switch
        {
            > 0 => ValueFlowDirection.Positive,
            < 0 => ValueFlowDirection.Negative,
            _ => ValueFlowDirection.None
        };
        diffPct = Mathf.Abs(diffPct);
        if (diffPct <= 0.0078125f) return false;
        return totalDiff >= minDiffMargin;
    }

    public static bool NeedsEqualizing<T>(BaseContainer<T> containerA, BaseContainer<T> containerB, T def, float minDiffMargin, out ValueFlowDirection flow, out float diffPct) where T : FlowValueDef
    {
        flow = ValueFlowDirection.None;
        diffPct = 0f;
        //if (roomA.IsOutdoors && roomB.IsOutdoors) return false;
        var fromTotal = containerA.TotalStoredOf(def);
        var toTotal = containerB.TotalStoredOf(def);

        var fromPct = containerA.StoredPercentOf(def);
        var toPct = containerB.StoredPercentOf(def);

        var totalDiff = Mathf.Abs(fromTotal - toTotal);
        diffPct = fromPct - toPct;

        flow = diffPct switch
        {
            > 0 => ValueFlowDirection.Positive,
            < 0 => ValueFlowDirection.Negative,
            _ => ValueFlowDirection.None
        };
        diffPct = Mathf.Abs(diffPct);
        if (diffPct <= 0.0078125f) return false;
        return totalDiff >= minDiffMargin;
    }

    //
    public static void TryEqualizeAll<T>(BaseContainer<T> from, BaseContainer<T> to) where T : FlowValueDef
    {
        if (!NeedsEqualizing(from, to, MIN_EQ_VAL, out var flow, out var diffPct))
        {
            return;
        }

        BaseContainer<T> sender   = flow == ValueFlowDirection.Positive ? from : to;
        BaseContainer<T> receiver = flow == ValueFlowDirection.Positive ? to : from;

        foreach (var valueDef in sender.AllStoredTypes)
        {
            var value = sender.TotalStoredOf(valueDef) * 0.5f;
            var flowAmount = sender.GetMaxTransferRateTo(receiver, valueDef, Mathf.CeilToInt(value * diffPct * valueDef.FlowRate));
            if (sender.TryTransferTo(receiver, valueDef, flowAmount, out _))
            {
                //...
            }
        }
    }
    
    public static void TryEqualize<T>(BaseContainer<T> from, BaseContainer<T> to, T valueDef) where T : FlowValueDef
    {
        if (!NeedsEqualizing(from, to, valueDef, MIN_EQ_VAL, out var flow, out var diffPct))
        {
            return;
        }

        BaseContainer<T> sender   = flow == ValueFlowDirection.Positive ? from : to;
        BaseContainer<T> receiver = flow == ValueFlowDirection.Positive ? to : from;

        //Get base transfer part
        var value = sender.TotalStoredOf(valueDef) * 0.5f;
        var flowAmount = sender.GetMaxTransferRateTo(receiver, valueDef, Mathf.CeilToInt(value * diffPct * valueDef.FlowRate));

        //
        if (sender.TryTransferTo(receiver, valueDef, flowAmount, out _))
        {
            //...
        }
        /*
        if (sender.CanFullyTransferTo(receiver, valueDef, flowAmount))
        {
            if (sender.TryRemoveValue(valueDef, flowAmount, out float actualVal))
            {
                receiver.TryAddValue(valueDef, actualVal, out _);
            }
        }
        */
    }
}