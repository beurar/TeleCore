using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// Manages any Def as a numeric value in a stack.
    /// Only supports DefValues with TValue set as <see cref="float"/>.
    /// </summary>
    /// <typeparam name="TDef">The <see cref="Def"/> of the stack.</typeparam>
    public struct DefValueStack<TDef> : IEnumerable<DefValue<TDef, float>> where TDef : Def
    {
        private readonly float? _maxCapacity;
        private readonly OrderedDictionary<TDef, DefValue<TDef, float>> _values;
        private float _totalValue;

        //States
        public bool IsValid => _values != null;
        public bool Empty => _totalValue == 0f;
        public bool? Full => _maxCapacity == null ? null :  _totalValue >= _maxCapacity;
        
        //Stack Info
        public IEnumerable<TDef> AllTypes => _values.Keys;
        public float TotalValue => _totalValue;
        public int Length => _values.Count;

        public DefValue<TDef, float> this[int index] => _values[index];

        public DefValue<TDef, float> this[TDef def]
        {
            get => _values[def];
            private set => TryAddOrSet(value);
        }

        private void TryAddOrSet(DefValue<TDef, float> value)
        {
            //Get previous value
            var previous = _values[value.Def];
            
            //Set new value
            if (_values.Contains(value.Def))
            {
                _values[value.Def] = value;
            }
            else
            {
                _values.Add(value.Def, value);
            }

            //Get Delta
            var delta = previous - _values[value.Def];
            _totalValue += delta.Value;
        }
        
        public DefValueStack(float? maxCapacity = null)
        {
            _maxCapacity = maxCapacity;
        }
        
        public DefValueStack(IDictionary<TDef, float> source, float? maxCapacity = null) : this(maxCapacity)
        {
            _values = new OrderedDictionary<TDef, DefValue<TDef, float>>();
            foreach (var value in source)
            {
                _values.Add(value.Key, new DefValue<TDef, float>(value.Key, value.Value));
                _totalValue += value.Value;
            }
        }

        public static DefValueStack<TDef> Invalid => new(null, null)
        {
            _totalValue = -1,
        };
        
        public override string ToString()
        {
            if (_values == null || Length == 0) return "Empty Stack";
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < Length; i++)
            {
                var value = _values[i];
                sb.AppendLine($"[{i}] {value}");
            }
            return sb.ToString();
        }

        public IEnumerator<DefValue<TDef, float>> GetEnumerator()
        {
            return _values.Values.GetEnumerator();
        }

        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        //Math
        #region DefValue Math

        public static DefValueStack<TDef> operator +(DefValueStack<TDef> stack, DefValue<TDef, float> value)
        {
            stack[value.Def] += value;
            return stack;
        }

        public static DefValueStack<TDef> operator -(DefValueStack<TDef> stack, DefValue<TDef, float> value)
        {
            stack[value.Def] -= value;
            return stack;
        }


        #endregion

        #region Stack Math

        public static DefValueStack<TDef> operator +(DefValueStack<TDef> stack , DefValueStack<TDef> other)
        {
            foreach (var value in other._values.Keys)
            {
                stack[value] += other[value];
            }
            return stack;
        }

        public static DefValueStack<TDef> operator -(DefValueStack<TDef> stack , DefValueStack<TDef> other)
        {
            foreach (var value in other._values.Keys)
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

        #endregion
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
}
