using System;
using TeleCore.Defs;
using UnityEngine;

namespace TeleCore.Network.Flow.Values;

/// <summary>
/// The resulting state of a FlowBox value-change operation.
/// </summary>
public enum ValueState
{
    Incomplete,
    Completed,
    CompletedWithExcess,
    CompletedWithShortage,
    Failed
}

/// <summary>
/// The result of a FlowBox operation.
/// </summary>
public struct FlowResult
{
    public ValueState State { get; private set; }
    
    public double DesiredAmount { get; private set; }
    public double ActualAmount { get; private set; }
    
    public FlowValueStack FullDiff { get; private set; }
    
    public static implicit operator bool(FlowResult result)
    {
        return result.State != ValueState.Failed;
    }

    public FlowResult() { }

    public FlowResult(FlowValueStack stack)
    {
        FullDiff = stack;
    }

    #region Resolver
    
    public static FlowResult Init(double desiredAmount)
    {
        return new FlowResult
        {
            State = ValueState.Incomplete,
            DesiredAmount = desiredAmount,
            ActualAmount = 0,
        };
    }
    
    public FlowResult Fail()
    {
        State = ValueState.Failed;
        return this;
    }
    
    public FlowResult Complete(double? finalActual = null)
    {
        State = ValueState.Completed;
        ActualAmount = finalActual ?? ActualAmount;
        return this;
    }

    public FlowResult Resolve()
    {
        if (Math.Abs(ActualAmount - DesiredAmount) < Mathf.Epsilon)
            State = ValueState.Completed;
        if (ActualAmount > DesiredAmount)
            State = ValueState.CompletedWithExcess;
        if (ActualAmount < DesiredAmount)
            State = ValueState.CompletedWithShortage;
        return this;
    }

    

    #endregion
}