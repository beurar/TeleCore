using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore;

//Implementer - glues both the holder and the container together and exposes a Container Property
public interface IContainerImplementer<TValue, THolder, out TContainer>
    where TValue : FlowValueDef
    where THolder : ITestHolder
    where TContainer : ValueContainer<TValue, THolder>
{
    public TContainer Container { get; }
}

//Base holder provides settings for the container
public interface ITestHolder
{
    public string ContainerTitle { get; }
}

//Raw Implementation in a class, using all base types
public class ClassWithContainer : IContainerImplementer<FlowValueDef, ITestHolder, ValueContainer<FlowValueDef, ITestHolder>>
{
    public ValueContainer<FlowValueDef, ITestHolder> Container { get; }
}


//A standardized implementation, using a derived Container class
public class FixedContainerImplementer : IContainerImplementer<FlowValueDef, ITestHolder, FixedValueContainer>
{
    public FixedValueContainer Container { get; }

    public void Foo()
    {
        var newContainer = new FixedContainerImplementer();
        if (ValueTools.NeedsEqualizing(Container, newContainer.Container, out _, out _))
        {
            
        }
    }
}

public static class ValueTools
{
    public const float MIN_EQ_VAL = 2;
    public const float MIN_FLOAT_COMPARE = 0.01f; //0.00390625F; //0.001953125F;
    
    /// <summary>
    /// Checks whether or not two containers need to be equalized.
    /// </summary>
    /// <param name="flow">The flow direction output relative to the first container.</param>
    /// <param name="diffPct">The difference in content by percentage.</param>
    public static bool NeedsEqualizing<T>(ValueContainerBase<T> from, ValueContainerBase<T> to, out ValueFlowDirection flow, out float diffPct) where T : FlowValueDef
    {
        flow = ValueFlowDirection.None;
        diffPct = 0f;
        
        var fromPct = from.StoredPercent;
        var toPct   = to.StoredPercent;
        
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
       
    }

    //
    public static void TryEqualizeAll<TValue>(ValueContainerBase<TValue> from, ValueContainerBase<TValue> to) where TValue : FlowValueDef
    {
        if (!NeedsEqualizing<TValue>(from, to, out var flow, out var diffPct))
        {
            return;
        }

        var sender   = (flow == ValueFlowDirection.Positive ? from : to);
        var receiver = (flow == ValueFlowDirection.Positive ? to : from);

        var tempTypes = StaticListHolder<TValue>.RequestSet("EqualizingTempSet");
        tempTypes.AddRange(sender.AllStoredTypes);
        
        var smoothVal = receiver.Capacity * 0.1f * diffPct;
        foreach (var valueDef in tempTypes)
        {
            smoothVal = (smoothVal * valueDef.FlowRate) / sender.ValueStack.Length;
            smoothVal = receiver.GetMaxTransferRate(valueDef, smoothVal);
            _ = sender.TryTransferTo(receiver, valueDef, smoothVal, out _); 
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