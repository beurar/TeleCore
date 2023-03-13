using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RimWorld;
using TeleCore.Static.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Verse;

namespace TeleCore
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct UnsafeDefValueStack<TDef>
        where TDef : Def
    {
         [FieldOffset(0)]
        private fixed ushort _stackPart1[4096];
        
        [FieldOffset(8192)]//2*4095
        private fixed float _stackPart2[4096];

        public DefFloat<TDef> At(int index)
        {
            return new DefFloat<TDef>(_stackPart1[index].ToDef<TDef>(), _stackPart2[index]);
        }
    }
    
    /// <summary>
    /// Manages any Def as a numeric value in a stack.
    /// </summary>
    /// <typeparam name="TDef">The <see cref="Def"/> of the stack.</typeparam>
    public struct DefValueStack<TDef> where TDef : Def
    {
        private float? _maxCapacity;
        private DefFloat<TDef>[] _stack;
        private float _totalValue;
        
        //States
        public bool IsValid => _stack != null;
        public bool Empty => _totalValue == 0f;
        public bool? Full => _maxCapacity == null ? null :  _totalValue >= _maxCapacity;
        
        //Stack Info
        public IEnumerable<TDef> Defs => _stack.Select(value => value.Def);
        public IEnumerable<DefFloat<TDef>> Values => _stack;
        
        public float TotalValue => _totalValue;
        public int Length => _stack.Length;

        public DefFloat<TDef> this[int index] => _stack[index];

        public DefFloat<TDef> this[TDef def]
        {
            get => TryGetWithFallback(def, new DefFloat<TDef>(def, 0));
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
        
        public DefValueStack(IDictionary<TDef, float> source, float? maxCapacity = null) : this(maxCapacity)
        {
            if (source.EnumerableNullOrEmpty())
            {
                TLog.Warning($"[{GetType()}.ctor]Tried to create new DefValueStack from NullOrEmpty source dictionary.");
                Default(maxCapacity);
                return;
            }
            
            _stack = new DefFloat<TDef>[source.Count];
            var i = 0;
            foreach (var value in source)
            {
                _stack[i] = new DefFloat<TDef>(value.Key, value.Value);
                _totalValue += value.Value;
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
            
            _stack = new DefFloat<TDef>[defs.Count];
            var i = 0;
            foreach (var def in defs)
            {
                _stack[i] = new DefFloat<TDef>(def, 0);
                i++;
            }
        }
        
        public DefValueStack(DefFloat<TDef>[] source, float? maxCapacity = null) : this(maxCapacity)
        {
            if (source.NullOrEmpty())
            {
                TLog.Warning($"[{GetType()}.ctor]Tried to create new DefValueStack from NullOrEmpty DefFloat Array.");
                Default(maxCapacity);
                return;
            }
            
            _stack = new DefFloat<TDef>[source.Length];
            var i = 0;
            foreach (var value in source)
            {
                _stack[i] = new DefFloat<TDef>(value, value.Value);
                _totalValue += value.Value;
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
            
            _stack = new DefFloat<TDef>[other._stack.Length];
            for (var i = 0; i < _stack.Length; i++)
            {
                _stack[i] = other._stack[i];
            }
            _totalValue = other._totalValue;
        }

        private void Default(float? maxCapacity = null)
        {
            _maxCapacity = maxCapacity;
            _stack = Array.Empty<DefFloat<TDef>>();
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
        
        public DefFloat<TDef> TryGetWithFallback(TDef key, DefFloat<TDef> fallback)
        {
            return TryGetValue(key, out _, out var value) ? value : fallback;
        }

        public bool TryGetValue(TDef key, out int index, out DefFloat<TDef> value)
        {
            index = -1;
            value = new DefFloat<TDef>(key, float.NaN);
            for (var i = 0; i < _stack.Length; i++)
            {
                value = _stack[i];
                if (value.Def != key) continue;
                index = i;
                return true;
            }
            return false;
        }

        private void TryAddOrSet(DefFloat<TDef> newValue)
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

        public IEnumerator<DefFloat<TDef>> GetEnumerator()
        {
            return _stack.Cast<DefFloat<TDef>>().GetEnumerator(); //(IEnumerator<DefFloat<TDef>>)_stack.GetEnumerator();
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

        public static DefValueStack<TDef> operator +(DefValueStack<TDef> stack, DefFloat<TDef> value)
        {
            stack = new DefValueStack<TDef>(stack);
            stack[value.Def] += value;
            return stack;
        }

        public static DefValueStack<TDef> operator -(DefValueStack<TDef> stack, DefFloat<TDef> value)
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
        
        #endregion

        #region Comparision

        public static bool operator <(DefValueStack<TDef> stack , float value)
        {
            return stack._totalValue < value;
        }

        public static bool operator >(DefValueStack<TDef> stack, float value)
        {
            return stack._totalValue > value;
        }

        public static bool operator ==(DefValueStack<TDef> stack, float value)
        {
            return Math.Abs(stack._totalValue - value) < 0.001;
        }

        public static bool operator !=(DefValueStack<TDef> stack, float value)
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
            return Nullable.Equals(_maxCapacity, other._maxCapacity) && _stack.Equals(other._stack) && _totalValue.Equals(other._totalValue);
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
}

    /*
    public struct DefValueStack<TDef> where TDef : Def
    {
        public DefValue<TDef, float>[] values;
        private readonly HashSet<TDef> knownTypes;
        private float totalValue = 0;

        public bool IsValid => values != null || knownTypes != null;
        public bool Empty => totalValue == 0f;
        
        public float TotalValue => totalValue;
        public IEnumerable<TDef> AllTypes => knownTypes;

        public int Length => values.Length; 
        
        public DefValue<TDef, float> this[TDef def]
        {
            get { return values.FirstOrFallback(v => v.Def == def, new DefValue<TDef, float>(def, 0)); }
            //set { values.SetValue(value, values.FirstIndexOf(v => v.Def == def)); }
        }

        public DefValueStack(Dictionary<TDef, float> values)
        {
            this.values = new DefValue<TDef, float>[values.Count];
            this.values.Populate(values.Select(t => new DefValue<TDef, float>(t.Key, t.Value)));

            knownTypes = new HashSet<TDef>();
            knownTypes.AddRange(values.Keys);

            totalValue = values.Sum(v => v.Value);
        }

        public DefValueStack(TDef def, float val)
        {
            values = new DefValue<TDef, float>[1];
            values[0] = new DefValue<TDef, float>(def, val);
            //color = Color.white;

            knownTypes = new HashSet<TDef>();
            knownTypes.Add(def);

            totalValue = val;
        }

        public DefValueStack()
        {
            values = null;
            knownTypes = new HashSet<TDef>();
        }

        public void PushNew(TDef def, float value)
        {
            var previousValues = values;
            if (knownTypes.Add(def))
            {
                //Create Stack
                if (values.NullOrEmpty())
                {
                    values = new DefValue<TDef, float>[1];
                    values[0] = new DefValue<TDef, float>(def, 0);
                }
                else
                {
                    //Push new onto Stack
                    values = new DefValue<TDef, float>[values.Length + 1];
                    values.Populate(previousValues);
                    values[values.Length - 1] = new DefValue<TDef, float>(def, 0);
                }
            }

            //Add To Stack
            this += new DefValue<TDef, float>(def, value);
        }

        public void Reset()
        {
            values = null;
            totalValue = 0;
            knownTypes?.Clear();
        }

        public bool HasValue(TDef def)
        {
            return knownTypes.Contains(def);
        }

        //
        public static DefValueStack<TDef> operator +(DefValueStack<TDef> a, DefValueStack<TDef> b)
        {
            if (a.values == null)
                return b;
            if (b.values == null)
                return a;

            for (var i = 0; i < a.values.Length; i++)
            {
                var valueA = a.values[i];
                for (var k = 0; k < b.values.Length; k++)
                {
                    var valueB = b.values[k];
                    if (valueA.Def.Equals(valueB.Def))
                    {
                        a.values[i] += valueB;
                    }
                }
            }

            return a;
        }

        public static DefValueStack<TDef> operator +(DefValueStack<TDef> a, DefValue<TDef, float> b)
        {
            if (!a.HasValue(b.Def))
            {
                a.PushNew(b.Def, b.Value);
            }
            for (int i = 0; i < a.values.Length; i++)
            {
                var value = a.values[i];
                if (value.Def != b.Def) continue;
                a.values[i] = new DefValue<TDef, float>(b.Def, Mathf.Clamp(value.Value + b.Value, 0, float.MaxValue));
                a.totalValue = Mathf.Clamp(a.totalValue + b.Value, 0, float.MaxValue);
                break;
            }
            return a;
        }

        public static DefValueStack<TDef> operator -(DefValueStack<TDef> minuend , DefValueStack<TDef> subtrahend)
        {
            //Subtract other from 
            for (int i = 0; i < subtrahend.Length; i++)
            {

                var value = subtrahend.values[i];
                minuend -= subtrahend[value.Def];
            }
            return minuend;
        }
        
        public static DefValueStack<TDef> operator -(DefValueStack<TDef> a, DefValue<TDef, float> b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                var value = a.values[i];
                if (value.Def != b.Def) continue;
                a.values[i] = new DefValue<TDef, float>(b.Def, Mathf.Clamp(value.Value - b.Value, 0, float.MaxValue));
                a.totalValue = Mathf.Clamp(a.totalValue - b.Value, 0, float.MaxValue);
                break;
            }
            return a;
        }

        public override string ToString()
        {
            if (values.NullOrEmpty()) return "Empty Stack";
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                sb.AppendLine($"[{i}] {value}");
            }
            return sb.ToString();
        }
    }
    */
