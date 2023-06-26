using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore.Defs;
using TeleCore.Generics.Container;
using TeleCore.Network.Flow.Values;
using UnityEngine;

namespace TeleCore.Network.Flow;

/// <summary>
/// The logical handler for fluid flow.
/// Area and height define the total content, elevation allows for flow control.
/// </summary>
public class FlowBox
{
    private const int AREA_VALUE = 100;
    
    //Config
    private readonly int _area;
    private readonly int _heigth;
    private readonly int _elevation;

    //
    private double _flowRate;

    private FlowValueStack _mainStack;
    private FlowValueStack _prevStack;
    
    public FlowValueStack Stack => _mainStack;
    
    public FlowValueStack PrevStack
    {
        get => _prevStack;
        set => _prevStack = value;
    }

    public double FlowRate
    {
        get => _flowRate;
        set => _flowRate = value;
    }

    public double TotalValue => _mainStack.TotalValue;
    public double MaxCapacity => _area * _heigth * AREA_VALUE;

    public double FillHeight => (TotalValue / MaxCapacity) * _heigth;
    public double FillPercent => TotalValue /MaxCapacity;
    
    public ContainerFillState FillState
    {
        get
        {
            return TotalValue switch
            {
                0 => ContainerFillState.Empty,
                var n when n >= MaxCapacity => ContainerFillState.Full,
                _ => ContainerFillState.Partial
            };
        }
    }

    #region Data Getters
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CapacityOf(FlowValueDef? def)
    {
        return MaxCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double StoredValueOf(FlowValueDef? def)
    {
        if (def == null) return 0;
        return Stack[def].Value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double TotalStoredOfMany(IEnumerable<FlowValueDef> defs)
    {
        return defs.Sum(StoredValueOf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float StoredPercentOf(FlowValueDef def)
    {
        return (float) (StoredValueOf(def) / Math.Ceiling(CapacityOf(def)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFull(FlowValueDef def)
    {
        if (def.sharesCapacity) return StoredValueOf(def) >= CapacityOf(def);
        return FillState == ContainerFillState.Full;
    }
    
    #endregion

    public FlowBox(int area, int height, int elevation)
    {
        _area = area;
        _heigth = height;
        _elevation = elevation;
    }

    public FlowValueStack RemoveContent(double moveAmount)
    {
        return _mainStack * moveAmount;
    }

    public void AddContent(FlowValueStack fullDiff)
    {
        _mainStack += fullDiff;
    }
}