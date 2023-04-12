using UnityEngine;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// Wraps any <see cref="Def"/> Type into a struct, attaching a numeric value
    /// </summary>
    /// <typeparam name="TDef">The <see cref="Def"/> Type of the value.</typeparam>
    /// <typeparam name="TValue">A numeric Type, can be <see cref="int"/> or <see cref="float"/></typeparam>
    public struct DefValueGeneric<TDef, TValue> 
        where TDef : Def
        where TValue : struct
    {
        public TDef Def { get; private set; }
        public TValue Value { get; set; }
        
        public static explicit operator DefValueGeneric<TDef,TValue>(DefValueLoadable<TDef,TValue> defValue)
        {
            return new DefValueGeneric<TDef,TValue>(defValue);
        }
        
        public static implicit operator DefValueGeneric<TDef, TValue>((TDef Def, TValue Value) value) => new (value.Def, value.Value);
        public static implicit operator TDef(DefValueGeneric<TDef, TValue> value) => value.Def;
        public static explicit operator TValue(DefValueGeneric<TDef, TValue> value) => value.Value;

        public DefValueGeneric(DefValueLoadable<TDef,TValue> defValue)
        {
            this.Def = defValue.Def;
            this.Value = defValue.Value;
        }

        public DefValueGeneric(TDef def, TValue value)
        {
            this.Def = def;
            this.Value = value;
        }

        //Float
        public static DefValueGeneric<TDef, float> operator +(DefValueGeneric<TDef, TValue> a, float b)
        {
            var value1 = a.Value switch
            {
                float f1 => f1,
                int i1 => i1,
                _ => 0
            };

            return new DefValueGeneric<TDef, float>(a.Def, value1 + b);
        }

        public static DefValueGeneric<TDef, float> operator -(DefValueGeneric<TDef, TValue> a, float b)
        {
            var value1 = a.Value switch
            {
                float f1 => f1,
                int i1 => i1,
                _ => 0
            };

            return new DefValueGeneric<TDef, float>(a.Def, value1 - b);
        }

        public static DefValueGeneric<TDef,float> operator +(DefValueGeneric<TDef,TValue> a, DefValueGeneric<TDef,float> b)
        {
            return new DefValueGeneric<TDef, float>
            {
                Def = a.Def ?? b.Def,
                Value = (a + b.Value).Value
            };
        }
        
        public static DefValueGeneric<TDef, float> operator -(DefValueGeneric<TDef, TValue> a, DefValueGeneric<TDef, float> b)
        {
            return new DefValueGeneric<TDef, float>
            {
                Def = a.Def ?? b.Def,
                Value = (a - b.Value).Value
            };
        }

        //Integer
        public static DefValueGeneric<TDef, int> operator +(DefValueGeneric<TDef, TValue> a, int b)
        {
            var value1 = a.Value switch
            {
                float f1 => Mathf.RoundToInt(f1),
                int i1 => i1,
                _ => 0
            };

            return new DefValueGeneric<TDef, int>(a.Def, value1 + b);
        }

        public static DefValueGeneric<TDef, int> operator -(DefValueGeneric<TDef, TValue> a, int b)
        {
            return a + (-b);
        }

        public static DefValueGeneric<TDef, int> operator +(DefValueGeneric<TDef, TValue> a, DefValueGeneric<TDef, int> b)
        {
            return a + b.Value;
        }

        public static DefValueGeneric<TDef, int> operator -(DefValueGeneric<TDef, TValue> a, DefValueGeneric<TDef, int> b)
        {
            return a - b.Value;
        }

        public override string ToString()
        {
            return $"(({Def.GetType()}):{Def}, {Value})";
        }
    }
}
