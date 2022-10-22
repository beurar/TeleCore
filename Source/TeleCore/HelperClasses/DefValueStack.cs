using System;
using System.Collections.Generic;
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
    /// </summary>
    /// <typeparam name="T">The <see cref="Def"/> of the stack.</typeparam>
    public struct DefValueStack<T> where T : Def
    {
        public DefValue<T, float>[] values;
        public readonly HashSet<T> knownTypes;
        private float totalValue = 0;

        public bool IsValid => values != null || knownTypes != null;
        public bool Empty => totalValue == 0f;

        public float TotalValue => totalValue;
        public IEnumerable<T> AllTypes => knownTypes;

        public DefValue<T, float> this[T def]
        {
            get { return values.FirstOrFallback(v => v.Def == def, new DefValue<T, float>(def, 0)); }
            //set { values.SetValue(value, values.FirstIndexOf(v => v.Def == def)); }
        }

        public DefValueStack(Dictionary<T, float> values)
        {
            this.values = new DefValue<T, float>[values.Count];
            this.values.Populate(values.Select(t => new DefValue<T, float>(t.Key, t.Value)));

            knownTypes = new HashSet<T>();
            knownTypes.AddRange(values.Keys);

            totalValue = values.Sum(v => v.Value);
        }

        public DefValueStack(T def, float val)
        {
            values = new DefValue<T, float>[1];
            values[0] = new DefValue<T, float>(def, val);
            //color = Color.white;

            knownTypes = new HashSet<T>();
            knownTypes.Add(def);

            totalValue = val;
        }

        public DefValueStack()
        {
            values = null;
            knownTypes = new HashSet<T>();
        }

        public void PushNew(T def, float value)
        {
            var previousValues = values;
            if (knownTypes.Add(def))
            {
                //Create Stack
                if (values.NullOrEmpty())
                {
                    values = new DefValue<T, float>[1];
                    values[0] = new DefValue<T, float>(def, 0);
                }
                else
                {
                    //Push new onto Stack
                    values = new DefValue<T, float>[values.Length + 1];
                    values.Populate(previousValues);
                    values[values.Length - 1] = new DefValue<T, float>(def, 0);
                }
            }

            //Add To Stack
            this += new DefValue<T, float>(def, value);
        }

        public void Reset()
        {
            values = null;
            totalValue = 0;
            knownTypes?.Clear();
        }

        public bool HasValue(T def)
        {
            return knownTypes.Contains(def);
        }

        //
        public static DefValueStack<T> operator +(DefValueStack<T> a, DefValueStack<T> b)
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

        public static DefValueStack<T> operator +(DefValueStack<T> a, DefValue<T, float> b)
        {
            if (!a.HasValue(b.Def))
            {
                a.PushNew(b.Def, b.Value);
            }
            for (int i = 0; i < a.values.Length; i++)
            {
                var value = a.values[i];
                if (value.Def != b.Def) continue;
                a.values[i] = new DefValue<T, float>(b.Def, Mathf.Clamp(value.Value + b.Value, 0, float.MaxValue));
                a.totalValue = Mathf.Clamp(a.totalValue + b.Value, 0, float.MaxValue);
                break;
            }
            return a;
        }

        public static DefValueStack<T> operator -(DefValueStack<T> a, DefValue<T, float> b)
        {
            for (int i = 0; i < a.values.Length; i++)
            {
                var value = a.values[i];
                if (value.Def != b.Def) continue;
                a.values[i] = new DefValue<T, float>(b.Def, Mathf.Clamp(value.Value - b.Value, 0, float.MaxValue));
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
}
