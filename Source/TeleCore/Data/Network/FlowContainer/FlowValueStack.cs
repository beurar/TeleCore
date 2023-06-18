using System;

namespace TeleCore.Network;

/// <summary>
/// Redundant replication 
/// </summary>
public struct FlowValueStack
{
    private readonly int _maxCapacity;
    private FlowValue[] _stack;
    private int _totalValue;
    
    public int Length => _stack.Length;
    public int TotalValue => _totalValue;
    public bool IsValid => _stack != null;
    public bool Empty => _totalValue == 0;
    public bool? Full
    {
        get
        {
            if (_maxCapacity == -1) return null;
            return _totalValue >= _maxCapacity;
        }
    }
    
    public FlowValue this[int index] => _stack[index];

    public FlowValue this[FlowValueDef def]
    {
        get => TryGetWithFallback(def, new FlowValue(def, 0));
        private set => TryAddOrSet(value);
    }

    public FlowValueStack()
    {
        _stack = Array.Empty<FlowValue>();
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
            
        _stack = new FlowValue[other._stack.Length];
        for (var i = 0; i < _stack.Length; i++)
        {
            _stack[i] = other._stack[i];
        }
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
            //Set new value
            index = _stack.Length;
            Array.Resize(ref _stack, _stack.Length + 1);
            var oldArr = _stack;
            for (int i = 0; i < index; i++)
            {
                _stack[i] = oldArr[i];
            }
        }

        _stack[index] = newValue;
            
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

    #region Stack Math

    public static FlowValueStack operator +(FlowValueStack stack, FlowValueStack other)
    {
        stack = new FlowValueStack(stack);
        foreach (var value in other._stack)
        {
            stack[value] += value;
        }
        return stack;
    }

    public static FlowValueStack operator -(FlowValueStack stack, FlowValueStack other)
    {
        stack = new FlowValueStack(stack);
        foreach (var value in other._stack)
        {
            stack[value] -= other[value];
        }
        return stack;
    }

    public static FlowValueStack operator /(FlowValueStack stack, int split)
    {
        stack = new FlowValueStack(stack);
        for (var i = 0; i < stack.Length; i++)
        {
            stack._stack[i] = stack[i] / split;
        }
        return stack;
    }

    #endregion

    #region Comparision

    public static bool operator <(FlowValueStack stack, int value)
    {
        return stack._totalValue < value;
    }

    public static bool operator >(FlowValueStack stack, int value)
    {
        return stack._totalValue > value;
    }

    public static bool operator ==(FlowValueStack stack, int value)
    {
        return stack._totalValue == value;
    }

    public static bool operator !=(FlowValueStack stack, int value)
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
        return obj is FlowValueStack other && Equals(other);
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