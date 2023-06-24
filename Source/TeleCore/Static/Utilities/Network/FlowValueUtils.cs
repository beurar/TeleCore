using TeleCore;
using TeleCore.Defs;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore.Network;

public static class FlowValueUtils
{
    internal const float MIN_EQ_VAL = 2;
    internal const float MIN_FLOAT_COMPARE = 0.01f; //0.00390625F; //0.001953125F;
    
    /// <summary>
    /// Checks whether or not two containers need to be equalized.
    /// </summary>
    /// <param name="flow">The flow direction output relative to the first container.</param>
    /// <param name="diffPct">The difference in content by percentage.</param>
    public static bool NeedsEqualizing<T, T2>(ValueContainerBase<T> containerA, ValueContainerBase<T2> containerB, out ValueFlowDirection flow, out float diffPct) 
        where T : FlowValueDef
        where T2 : FlowValueDef
    {
        flow = ValueFlowDirection.None;
        diffPct = 0f;
        
        var fromPct = containerA.StoredPercent;
        var toPct   = containerB.StoredPercent;
        
        diffPct = fromPct - toPct;
        flow = diffPct switch
        {
            > 0 => ValueFlowDirection.Positive,
            < 0 => ValueFlowDirection.Negative,
            _ => ValueFlowDirection.None
        };
        
        //diffPct = Mathf.Abs(diffPct);
        var relativeDiff = Mathf.Abs(diffPct / ((fromPct + toPct) / 2)); // relative difference calculation
        return relativeDiff >= 0.01f;
        //return Mathf.Abs(diffPct) >= MIN_FLOAT_COMPARE;
    }
    

    public static bool NeedsEqualizing2<T>(ValueContainerBase<T> containerA, ValueContainerBase<T> containerB, out ValueFlowDirection flow, out float diffPct) where T : FlowValueDef
    {
        return NeedsEqualizing(containerA, containerB, out flow, out diffPct);
        flow = ValueFlowDirection.None;
        diffPct = 0f;
        
        var fromPct = containerA.StoredPercent;
        var toPct   = containerB.StoredPercent;
        
        diffPct = fromPct - toPct;
        flow = diffPct switch
        {
            > 0 => ValueFlowDirection.Positive,
            < 0 => ValueFlowDirection.Negative,
            _ => ValueFlowDirection.None
        };
        
        //diffPct = Mathf.Abs(diffPct);
        var relativeDiff = Mathf.Abs(diffPct / ((fromPct + toPct) / 2)); // relative difference calculation
        return relativeDiff >= 0.01f;
        //return Mathf.Abs(diffPct) >= MIN_FLOAT_COMPARE;
    }

    public static bool NeedsEqualizing<T>(ValueContainerBase<T> containerA, ValueContainerBase<T> containerB, T def, float minDiffMargin, out ValueFlowDirection flow, out float diffPct) where T : FlowValueDef
    {
        flow = ValueFlowDirection.None;
        diffPct = 0f;
        //if (roomA.IsOutdoors && roomB.IsOutdoors) return false;
        var fromTotal = containerA.StoredValueOf(def);
        var toTotal = containerB.StoredValueOf(def);

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
        if (diffPct <= MIN_FLOAT_COMPARE) return false;
        return totalDiff >= minDiffMargin;
    }

    public static bool CanExchangeForbidden<TValue>(ValueContainerBase<TValue> holder, ValueContainerBase<TValue> receiver)
        where TValue : FlowValueDef
    {
        if (holder.FillState == ContainerFillState.Empty) return false;
        if (!holder.ContainsForbiddenType) return false;
        foreach (var type in holder.AcceptedTypes)
        {
            var filter = holder.GetFilterFor(type);
            var filterOther = receiver.GetFilterFor(type);
            if (!filter.canStore && filterOther.canReceive)
            {
                return true;
            }
        }

        return false;
    }
    
    //
    public static void TryEqualizeAll<TValue>(ValueContainerBase<TValue> from, ValueContainerBase<TValue> to) where TValue : FlowValueDef
    {
        if (!NeedsEqualizing(from, to, out var flow, out var diffPct))
        {
            return;
        }

        var sender   = (flow == ValueFlowDirection.Positive ? from : to);
        var receiver = (flow == ValueFlowDirection.Positive ? to : from);

        var tempTypes = StaticListHolder<TValue>.RequestSet("EqualizingTempSet");
        tempTypes.AddRange(sender.StoredDefs);
        
        var smoothVal = receiver.Capacity * 0.1f * diffPct;
        foreach (var valueDef in tempTypes)
        {
            smoothVal = (smoothVal * valueDef.FlowRate) / sender.ValueStack.Length;
            smoothVal = receiver.GetMaxTransferRate(valueDef, smoothVal);
            _ = sender.TryTransferValue(receiver, valueDef, smoothVal, out _); 
        }
        tempTypes.Clear();
    }
    
    public static void TryEqualize<TValue>(ValueContainerBase<TValue> from, ValueContainerBase<TValue> to, TValue valueDef) where TValue : FlowValueDef
    {
        if (!NeedsEqualizing(from, to, valueDef, MIN_EQ_VAL, out var flow, out var diffPct))
        {
            return;
        }

        var sender   = flow == ValueFlowDirection.Positive ? from : to;
        var receiver = flow == ValueFlowDirection.Positive ? to : from;

        //Get base transfer part
        var value = sender.StoredValueOf(valueDef) * 0.5f;
        var flowAmount = receiver.GetMaxTransferRate(valueDef, Mathf.CeilToInt(value * diffPct * valueDef.FlowRate));

        //
        if (sender.TryTransferValue(receiver, valueDef, flowAmount, out _))
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