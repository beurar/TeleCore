using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OneOf;
using RimWorld;
using TeleCore.Static.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Verse;

namespace TeleCore;

//TODO:
public unsafe struct UnsafeDefValueStack<TDef> where TDef : Def
{
    private fixed ushort _stackPart1[4096];
        
    private fixed float _stackPart2[4096];

    public DefValue<TDef> At(int index)
    {
        return new DefValue<TDef>(_stackPart1[index].ToDef<TDef>(), _stackPart2[index]);
    }
}
    
/// <summary>
/// Manages any Def as a numeric value in a stack.
/// </summary>
/// <typeparam name="TDef">The <see cref="Def"/> of the stack.</typeparam>
public struct DefValueStack<TDef> : IExposable
    where TDef : Def
{
    private OneOf<int, float>? _maxCapacity;
    private DefValue<TDef>[] _stack;
    private OneOf<int, float> _totalValue;
        
    //States
    public bool IsValid => _stack != null;
    public bool Empty => _totalValue.Match(t => t == 0, t1 => t1 == 0);
    public bool? Full
    {
        get
        {
            if (_maxCapacity == null) return null;
            var maxCap = _maxCapacity;
            return _totalValue.Match(
                i => i >= maxCap.Value.AsT0,
                f => f >= maxCap.Value.AsT1);
        }
    }

    //Stack Info
    public IEnumerable<TDef> Defs => _stack.Select(value => value.Def);
    public IEnumerable<DefValue<TDef>> Values => _stack;
        
    public OneOf<int, float> TotalValue => _totalValue;
    public int Length => _stack.Length;

    public DefValue<TDef> this[int index] => _stack[index];

    public DefValue<TDef> this[TDef def]
    {
        get => TryGetWithFallback(def, new DefValue<TDef>(def, 0));
        private set => TryAddOrSet(value);
    }

    public DefValueStack()
    {
        Default();
    }

    public DefValueStack(float? maxCapacity = null) : this()
    {
        Default(maxCapacity);
    }
        
    public DefValueStack(IDictionary<TDef, OneOf<int, float>> source, float? maxCapacity = null) : this(maxCapacity)
    {
        if (source.EnumerableNullOrEmpty())
        {
            Default(maxCapacity);
            return;
        }
            
        _stack = new DefValue<TDef>[source.Count];
        var i = 0;
        foreach (var value in source)
        {
            _stack[i] = new DefValue<TDef>(value.Key, value.Value);
            AdjustTotal(value.Value);
            i++;
        }
    }
        
    public DefValueStack(ICollection<TDef> defs, float? maxCapacity = null) : this(maxCapacity)
    {
        if (!defs.Any())
        {
            TLog.Warning($"[{GetType()}.ctor]Tried to create new DefValueStack from Empty {typeof(TDef)} Array.");
            Default(maxCapacity);
            return;
        }
            
        _stack = new DefValue<TDef>[defs.Count];
        var i = 0;
        foreach (var def in defs)
        {
            _stack[i] = new DefValue<TDef>(def, 0);
            i++;
        }
    }
        
    public DefValueStack(DefValue<TDef>[] source, float? maxCapacity = null) : this(maxCapacity)
    {
        if (source.NullOrEmpty())
        {
            TLog.Warning($"[{GetType()}.ctor]Tried to create new DefValueStack from NullOrEmpty DefFloat Array.");
            Default(maxCapacity);
            return;
        }
            
        _stack = new DefValue<TDef>[source.Length];
        var i = 0;
        foreach (var value in source)
        {
            _stack[i] = value;
            AdjustTotal(value.Value);
            i++;
        }
    }
        
    public DefValueStack(DefValueStack<TDef> other, float? maxCapacity = null) : this(maxCapacity)
    {
        if (!other.IsValid)
        {
            TLog.Warning($"[{GetType()}.ctor]Tried to create new DefValueStack from invalid DefValueStack.");
            Default(maxCapacity);
            return;
        }
            
        _stack = new DefValue<TDef>[other._stack.Length];
        for (var i = 0; i < _stack.Length; i++)
        {
            _stack[i] = other._stack[i];
        }
        _totalValue = other._totalValue;
    }
        
    public void ExposeData()
    {
        Scribe_Values.Look(ref _maxCapacity, "maxCapacity");
        Scribe_Values.Look(ref _totalValue, "totalValue");
        Scribe_Arrays.Look(ref _stack, "stack");
    }

    private void Default(float? maxCapacity = null)
    {
        _maxCapacity = maxCapacity;
        _stack = Array.Empty<DefValue<TDef>>();
        _totalValue = 0;
    }
        
    private int IndexOf(TDef def)
    {
        for (var i = 0; i < _stack.Length; i++)
        {
            if (_stack[i].Def == def) return i;
        }
        return -1;
    }
        
    public DefValue<TDef> TryGetWithFallback(TDef key, DefValue<TDef> fallback)
    {
        return TryGetValue(key, out _, out var value) ? value : fallback;
    }

    public bool TryGetValue(TDef key, out int index, out DefValue<TDef> value)
    {
        index = -1;
        value = new DefValue<TDef>(key, float.NaN);
        for (var i = 0; i < _stack.Length; i++)
        {
            value = _stack[i];
            if (value.Def != key) continue;
            index = i;
            return true;
        }
        return false;
    }

    private void TryAddOrSet(DefValue<TDef> newValue)
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
        AdjustTotal(delta.Value);
    }

    private void AdjustTotal(OneOf<int, float> value, bool isAddition = true)
    {
        _totalValue =_totalValue.Match(
            t0 => t0 + value.AsT0, 
            t1 => t1 + value.AsT1);
    }
        
    public override string ToString()
    {
        if (_stack == null || Length == 0) return "Empty Stack";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[TOTAL: {TotalValue}]");
        for (var i = 0; i < Length; i++)
        {
            var value = _stack[i];
            sb.AppendLine($"[{i}] {value}");
        }
        return sb.ToString();
    }

    public IEnumerator<DefValue<TDef>> GetEnumerator()
    {
        return _stack.Cast<DefValue<TDef>>().GetEnumerator(); //(IEnumerator<DefFloat<TDef>>)_stack.GetEnumerator();
    }

    public void Reset()
    {
        _totalValue = 0;
        if (_stack == null || _stack.Length == 0) return;
        for (var i = 0; i < _stack.Length; i++)
        {
            _stack[i].Value = 0;
        }
    }
        
    //
    public static DefValueStack<TDef> Invalid => new()
    {
        _totalValue = -1,
    };

    //Math
    #region DefValue Math

    public static DefValueStack<TDef> operator +(DefValueStack<TDef> stack, DefValue<TDef> value)
    {
        stack = new DefValueStack<TDef>(stack);
        stack[value.Def] += value;
        return stack;
    }

    public static DefValueStack<TDef> operator -(DefValueStack<TDef> stack, DefValue<TDef> value)
    {
        stack = new DefValueStack<TDef>(stack);
        stack[value.Def] -= value;
        return stack;
    }
    
    #endregion

    #region Stack Math

    public static DefValueStack<TDef> operator +(DefValueStack<TDef> stack , DefValueStack<TDef> other)
    {
        stack = new DefValueStack<TDef>(stack);
        foreach (var value in other._stack)
        {
            stack[value] += value;
        }
        return stack;
    }

    public static DefValueStack<TDef> operator -(DefValueStack<TDef> stack , DefValueStack<TDef> other)
    {
        stack = new DefValueStack<TDef>(stack);
        foreach (var value in other._stack)
        {
            stack[value] -= other[value];
        }
        return stack;
    }

    public static DefValueStack<TDef> operator /(DefValueStack<TDef> stack, OneOf<int, float> split)
    {
        stack = new DefValueStack<TDef>(stack);
        for (var i = 0; i < stack.Length; i++)
        {
            stack._stack[i] = stack[i] / split;
        }
        return stack;
    }

    #endregion

    #region Comparision

    public static bool operator <(DefValueStack<TDef> stack , OneOf<int, float> value)
    {
        return stack._totalValue.Match(
            t0 => t0 < value.AsT0,
            t1 => t1 < value.AsT1);
    }

    public static bool operator >(DefValueStack<TDef> stack, OneOf<int, float> value)
    {
        return stack._totalValue.Match(
            t0 => t0 > value.AsT0,
            t1 => t1 > value.AsT1);
    }

    public static bool operator ==(DefValueStack<TDef> stack, OneOf<int, float> value)
    {
        return stack._totalValue.Match(
            t0 => t0 == value.AsT0,
            t1 => Math.Abs(t1 - value.AsT1) < Mathf.Epsilon);
    }

    public static bool operator !=(DefValueStack<TDef> stack, OneOf<int, float> value)
    {
        return !(stack == value);
    }
    
    public static bool operator ==(DefValueStack<TDef> left, DefValueStack<TDef> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DefValueStack<TDef> left, DefValueStack<TDef> right)
    {
        return !(left == right);
    }

    public bool Equals(DefValueStack<TDef> other)
    {
        return Nullable.Equals(_maxCapacity, other._maxCapacity) &&
               _stack.Equals(other._stack) &&
               _totalValue.Match(t0 => t0 == other._totalValue.AsT0,
                   t1 => t1 == other._totalValue.AsT1);
    }

    public override bool Equals(object? obj)
    {
        return obj is DefValueStack<TDef> other && Equals(other);
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