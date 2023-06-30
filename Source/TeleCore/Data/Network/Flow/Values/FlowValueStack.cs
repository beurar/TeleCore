using System;
using System.Collections;
using System.Collections.Immutable;
using TeleCore.Defs;
using TeleCore.Primitive;
using UnityEngine;

namespace TeleCore.Network.Flow.Values;

/// <summary>
/// Redundant replication 
/// </summary>
public struct FlowValueStack
{
    private readonly double _maxCapacity;
    private ImmutableArray<FlowValue> _stack;
    private double _totalValue;
    
    public int Length => _stack.Length;
    public double TotalValue => _totalValue;
    public bool IsValid => _stack != null;
    public bool Empty => _totalValue == 0;
    
    public bool? Full
    {
        get
        {
            if (Math.Abs(_maxCapacity - (-1)) < Mathf.Epsilon) return null;
            return _totalValue >= _maxCapacity;
        }
    }

    public ImmutableArray<FlowValue> Values => _stack;
    
    public FlowValue this[int index] => _stack[index];

    public FlowValue this[FlowValueDef def]
    {
        get => TryGetWithFallback(def, new FlowValue(def, 0));
        private set => TryAddOrSet(value);
    }

    public FlowValueStack()
    {
        _stack = ImmutableArray<FlowValue>.Empty;
        _totalValue = 0;
    }

    public FlowValueStack(int maxCapacity = -1) : this()
    {
        _maxCapacity = maxCapacity;
    }

    public FlowValueStack(FlowValueStack other, int maxCapacity = -1) : this(maxCapacity)
    {
        if (!other.IsValid)
        {
            TLog.Warning($"[{GetType()}.ctor]Tried to create new FlowValueStack from invalid FlowValueStack.");
            return;
        }

        _stack = other._stack;
        _totalValue = other._totalValue;
    }

    public DefValueStack<FlowValueDef> ToValueStack => new();

    #region Helpers
    
    public FlowValue TryGetWithFallback(FlowValueDef key, FlowValue fallback)
    {
        return TryGetValue(key, out _, out var value) ? value : fallback;
    }

    public bool TryGetValue(FlowValueDef key, out int index, out FlowValue value)
    {
        index = -1;
        value = new FlowValue(key, 0);
        for (var i = 0; i < _stack.Length; i++)
        {
            value = _stack[i];
            if (value.Def != key) continue;
            index = i;
            return true;
        }
        return false;
    }
    
    private void TryAddOrSet(FlowValue newValue)
    {
        //Add onto stack
        if (!TryGetValue(newValue.Def, out var index, out var previous))
        {
            _stack = _stack.Add(newValue);
        }
        else
        {
            //Set new value
            _stack = _stack.Replace(previous, newValue);
        }
        
        //Get Delta
        var delta = _stack[index] - previous;
        _totalValue += delta.Value;
    }

    #endregion
    
    //Math
    #region DefValue Math

    public static FlowValueStack operator +(FlowValueStack stack, FlowValue value)
    {
        stack = new FlowValueStack(stack);
        stack[value.Def] += value;
        return stack;
    }

    public static FlowValueStack operator -(FlowValueStack stack, FlowValue value)
    {
        stack = new FlowValueStack(stack);
        stack[value.Def] -= value;
        return stack;
    }
    
    #endregion

    #region Double Math

    public static FlowValueStack operator *(FlowValueStack stack, double value)
    {
        stack = new FlowValueStack(stack);
        for (var i = 0; i < stack._stack.Length; i++)
        {
            var flowValue = stack._stack[i];
            stack[flowValue.Def] = flowValue * value;
        }
        return stack;
    }


    #endregion

    #region Stack Math

    public static FlowValueStack operator +(FlowValueStack stack, FlowValueStack other)
    {
        //stack = new FlowValueStack(stack);
        foreach (var value in other._stack)
        {
            stack[value] += value;
        }
        return stack;
    }

    public static FlowValueStack operator -(FlowValueStack stack, FlowValueStack other)
    {
        //stack = new FlowValueStack(stack);
        foreach (var value in other._stack)
        {
            stack[value] -= other[value];
        }
        return stack;
    }

    public static FlowValueStack operator /(FlowValueStack stack, int split)
    {
        //stack = new FlowValueStack(stack);
        for (var i = 0; i < stack.Length; i++)
        {
            //stack._stack[i] = stack[i] / split;
            stack._stack = stack._stack.Replace(stack._stack[i], stack[i] / split);
        }
        return stack;
    }

    #endregion

    #region Comparision

    public static bool operator <(FlowValueStack stack, double value)
    {
        return stack._totalValue < value;
    }

    public static bool operator >(FlowValueStack stack, double value)
    {
        return stack._totalValue > value;
    }

    public static bool operator ==(FlowValueStack stack, double value)
    {
        return stack._totalValue == value;
    }

    public static bool operator !=(FlowValueStack stack, double value)
    {
        return !(stack == value);
    }
    
    public static bool operator ==(FlowValueStack left, FlowValueStack right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FlowValueStack left, FlowValueStack right)
    {
        return !(left == right);
    }

    public bool Equals(FlowValueStack other)
    {
        return (_maxCapacity == other._maxCapacity) &&
               _stack.Equals(other._stack) &&
               _totalValue == other._totalValue;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;
        return Equals((FlowValueStack)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = _maxCapacity.GetHashCode();
            hashCode = (hashCode * 397) ^ _stack.GetHashCode();
            hashCode = (hashCode * 397) ^ _totalValue.GetHashCode();
            return hashCode;
        }
    }

    #endregion
}