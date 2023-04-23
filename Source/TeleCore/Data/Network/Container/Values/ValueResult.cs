using System;
using System.Collections.Generic;
using System.Text;
using TeleCore.FlowCore;
using UnityEngine;

namespace TeleCore.FlowCore;

/// <summary>
/// The result of a <see cref="ValueContainerBase{TValue}"/> Value-Change operation.
/// </summary>
public struct ValueResult<TValue>
where TValue : FlowValueDef
{
    public ValueState State { get; private set; }
    //Initial desire value
    public float DesiredAmount { get; private set; }
    //Actual resulting value
    public float ActualAmount { get; private set; }

    public float LeftOver => DesiredAmount - ActualAmount;
    public float Diff { get; private set; }

    public DefValueStack<TValue> FullDiff { get; private set; }
    
    public static implicit operator bool(ValueResult<TValue> result)
    {
        return result.State != ValueState.Failed;
    }

    public ValueResult()
    {
    }

    public static ValueResult<TValue> InitFail(float desiredAmount)
    {
        return new ValueResult<TValue>
        {
            State = ValueState.Failed,
            DesiredAmount = desiredAmount,
            ActualAmount = 0,
        };
    }

    public static ValueResult<TValue> Init(float desiredAmount, TValue usedDef)
    {
        return new ValueResult<TValue>
        {
            State = ValueState.Incomplete,
            DesiredAmount = desiredAmount,
            ActualAmount = 0,
            FullDiff = new DefValueStack<TValue>(usedDef.ToSingleItemList())
        };
    }

    public static ValueResult<TValue> Init(float desiredAmount, ICollection<TValue> usedDefs)
    {
        return new ValueResult<TValue>
        {
            State = ValueState.Incomplete,
            DesiredAmount = desiredAmount,
            ActualAmount = 0,
            FullDiff = new DefValueStack<TValue>(usedDefs)
        };
    }
    
    public ValueResult<TValue> AddDiff(TValue def, float diffAmount)
    {
        FullDiff += (def, diffAmount);
        Diff += diffAmount;
        return this;
    }
    
    public ValueResult<TValue> SetActual(float actual)
    {
        ActualAmount = actual;
        return this;
    }
    
    public ValueResult<TValue> Fail()
    {
        State = ValueState.Failed;
        return this;
    }

    public ValueResult<TValue> Complete(float? finalActual = null)
    {
        State = ValueState.Completed;
        ActualAmount = finalActual ?? ActualAmount;
        return this;
    }

    public ValueResult<TValue> Resolve()
    {
        if (Math.Abs(ActualAmount - DesiredAmount) < Mathf.Epsilon)
            State = ValueState.Completed;
        if (ActualAmount > DesiredAmount)
            State = ValueState.CompletedWithExcess;
        if (ActualAmount < DesiredAmount)
            State = ValueState.CompletedWithShortage;
        return this;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"State: {State} | ");
        sb.Append($"DesiredToActual: {DesiredAmount} -> {ActualAmount} | ");
        sb.Append($"Diff: {Diff}");
        return sb.ToString();
    }
}