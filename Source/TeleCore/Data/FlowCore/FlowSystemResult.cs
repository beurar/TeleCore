using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TeleCore.Primitive;
using UnityEngine;

namespace TeleCore.FlowCore;

public enum FlowState
{
    Failed,
    Completed,
    CompletedWithExcess,
    CompletedWithShortage,
}

public enum FlowFailureReason
{    
    None,
    TransferOverflow,
    TransferUnderflow,
    TriedToAddToFull,
    TriedToRemoveEmptyValue,
    TriedToConsumeMoreThanExists,
    UsedForbiddenValueDef,
    IllegalState
}

public enum FlowOperation
{
    Add,
    Remove,
    Transfer
}

public struct FlowResultStack<TDef>
    where TDef : FlowValueDef
{
    public DefValueStack<TDef, double> Desired { get; }
    public DefValueStack<TDef, double> Actual { get; private set; }
    public FlowFailureReason Reason { get; private set; }
    
    public DefValueStack<TDef, double> Diff => Desired - Actual;
    private double DiffValue => Desired.TotalValue - Actual.TotalValue;
    
    public FlowState State
    {
        get
        {
            if (Reason != FlowFailureReason.None)
                return FlowState.Failed;
            
            if (DiffValue <= double.Epsilon)
                return FlowState.Completed;
            if (DiffValue > 0)
                return FlowState.CompletedWithExcess;
            if (DiffValue < 0)
                return FlowState.CompletedWithShortage;
            
            return FlowState.Failed;
        }
    }
    
    public static implicit operator bool(FlowResultStack<TDef> result) => result.State != FlowState.Failed;

    private FlowResultStack(DefValueStack<TDef,double> desired)
    {
        Desired = desired;
    }
    
    public static FlowResultStack<TDef> Init(DefValueStack<TDef, double> desired, FlowOperation opType)
    {
        if(opType == FlowOperation.Remove)
            desired *= -1;
        return new FlowResultStack<TDef>(desired);
    }
    
    public FlowResultStack<TDef> AddResult(DefValue<TDef, double> result)
    {
        Actual += result;
        return this;
    }

    public FlowResultStack<TDef> AddResult(FlowResult<TDef, double> subResult)
    {
        Actual += (subResult.Def, subResult.Actual);
        return this;
    }
    
    public FlowResultStack<TDef> Fail(FlowFailureReason reason)
    {
        Reason = reason;
        return this;
    }
}

[DebuggerDisplay("{State}: '{Reason}' | [{Def}]{Actual}/{Actual}")]
public readonly struct FlowResult<TDef, TValue>
    where TDef : FlowValueDef
    where TValue : unmanaged
{
    public TDef Def { get; }
    public Numeric<TValue> Desired { get; }
    public Numeric<TValue> Actual { get; }
    public Numeric<TValue> Diff => Desired - Actual;
    
    public FlowFailureReason Reason { get; }

    public static implicit operator bool(FlowResult<TDef, TValue> result) => result.State != FlowState.Failed;
    
    public FlowState State
    {
        get
        {
            if (Reason != FlowFailureReason.None)
                return FlowState.Failed;
            
            if (Diff <= Numeric<TValue>.Epsilon)
                return FlowState.Completed;
            if (Diff > Numeric<TValue>.Zero)
                return FlowState.CompletedWithExcess;
            if (Diff < Numeric<TValue>.Zero)
                return FlowState.CompletedWithShortage;
            
            return FlowState.Failed;
        }
    }
    
    private FlowResult(TDef def, TValue desired, FlowFailureReason reason)
    {
        Def = def;
        Desired = desired;
        Actual = Numeric<TValue>.Zero;
        Reason = reason;
    }
    
    public FlowResult(TDef def, TValue desired, TValue actual)
    {
        Def = def;
        Desired = desired;
        Actual = actual;
    }
    
    public static FlowResult<TDef, TValue> InitFailed(TDef def, TValue desired, FlowFailureReason reason)
    {
        //Default constructor is NaN failure.
        return new FlowResult<TDef, TValue>(def, desired, reason);
    }
}