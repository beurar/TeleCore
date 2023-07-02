using System;
using TeleCore.Primitive;
using UnityEngine;

namespace TeleCore.FlowCore;

public enum FlowState
{
    Incomplete,
    Completed,
    CompletedWithExcess,
    CompletedWithShortage,
    Failed
}

public struct FlowResult<TDef, TValue>
    where TDef : FlowValueDef
    where TValue : struct
{
    public DefValueStack<TDef, TValue> Actual { get; private set; }
    public Numeric<TValue> Desired { get; private set; }
    public FlowState State { get; private set; }

    public static implicit operator bool(FlowResult<TDef, TValue> result) => result.State != FlowState.Failed;

    public FlowResult()
    {
    }

    public static FlowResult<TDef, TValue> Init(TValue desiredAmount)
    {
        return new FlowResult<TDef, TValue>
        {
            State = FlowState.Incomplete,
            Desired = desiredAmount,
            Actual = new DefValueStack<TDef,TValue>(),
        };
    }
    
    public FlowResult<TDef, TValue> Complete(DefValueStack<TDef, TValue> result)
    {
        State = FlowState.Completed;
        Actual = result;
        return this;
    }
    
    public FlowResult<TDef, TValue> Complete(DefValue<TDef, TValue> result)
    {
        State = FlowState.Completed;
        Actual += result;
        return this;
    }
    
    public FlowResult<TDef, TValue> Resolve()
    {
        if (MathG.Abs(Actual.TotalValue - Desired) < Numeric<TValue>.Epsilon)
            State = FlowState.Completed;
        if (Actual > Desired)
            State = FlowState.CompletedWithExcess;
        if (Actual < Desired)
            State = FlowState.CompletedWithShortage;
        return this;
    }
}