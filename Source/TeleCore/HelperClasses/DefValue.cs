using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class DefValueDef<TDef,TValue> where TDef : Def
    where TValue : struct
    {
        public TDef def;
        public TValue value;

        public TDef Def => def;
        public TValue Value
        {
            get => value;
            set => this.value = value;
        }

        public bool IsValid => def != null && value is float or int;

        public static implicit operator DefValue<TDef, TValue>(DefValueDef<TDef, TValue> d) => new(d.Def, d.value);
        public static explicit operator DefValueDef<TDef, TValue>(DefValue<TDef, TValue> d) => new (d.Def, d.Value);

        public DefValueDef() { }

        public DefValueDef(TDef def, TValue value)
        {
            this.def = def;
            this.value = value;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            //Listing
            if (xmlRoot.Name == "li")
            {
                var innerValue = xmlRoot.InnerText;
                string s = Regex.Replace(innerValue, @"\s+", "");
                string[] array = s.Split(',');
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(def)}", array[0]);
                value = ParseHelper.FromString<TValue>(array.Length > 1 ? array[1] : "1");
            }

            //InLined
            else
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(def)}", xmlRoot.Name);
                value = ParseHelper.FromString<TValue>(xmlRoot.FirstChild.Value);
            }
        }

        public override string ToString()
        {
            return $"{def?.defName}: {value}";
        }

        public string ToStringPercent()
        {
            if (value is not float f) return ToString();
            return $"{def?.defName}: ({f.ToStringPercent()})";
        }
    }

    /// <summary>
    /// Wraps any <see cref="Def"/> Type into a struct, attaching a numeric value
    /// </summary>
    /// <typeparam name="TDef">The <see cref="Def"/> Type of the value.</typeparam>
    /// <typeparam name="TValue">A numeric Type, can be <see cref="int"/> or <see cref="float"/></typeparam>
    public struct DefValue<TDef, TValue> 
        where TDef : Def 
        where TValue : struct
    {
        public TDef Def { get; private set; }
        public TValue Value { get; set; }
        
        public static explicit operator DefValue<TDef,TValue>(DefValueDef<TDef,TValue> defValue)
        {
            return new DefValue<TDef,TValue>(defValue);
        }
        public static implicit operator DefValue<TDef, TValue>((TDef Def, TValue Value) value) => new (value.Def, value.Value);

        public static implicit operator TDef(DefValue<TDef, TValue> value) => value.Def;
        public static explicit operator TValue(DefValue<TDef, TValue> value) => value.Value;

        public DefValue(DefValueDef<TDef,TValue> defValue)
        {
            this.Def = defValue.def;
            this.Value = defValue.value;
        }

        public DefValue(TDef def, TValue value)
        {
            this.Def = def;
            this.Value = value;
        }

        //Float
        public static DefValue<TDef, float> operator +(DefValue<TDef, TValue> a, float b)
        {
            var value1 = a.Value switch
            {
                float f1 => f1,
                int i1 => i1,
                _ => 0
            };

            return new DefValue<TDef, float>(a.Def, value1 + b);
        }

        public static DefValue<TDef, float> operator -(DefValue<TDef, TValue> a, float b)
        {
            var value1 = a.Value switch
            {
                float f1 => f1,
                int i1 => i1,
                _ => 0
            };

            return new DefValue<TDef, float>(a.Def, value1 - b);
        }

        public static DefValue<TDef,float> operator +(DefValue<TDef,TValue> a, DefValue<TDef,float> b)
        {
            return new DefValue<TDef, float>
            {
                Def = a.Def ?? b.Def,
                Value = (a + b.Value).Value
            };
        }
        
        public static DefValue<TDef, float> operator -(DefValue<TDef, TValue> a, DefValue<TDef, float> b)
        {
            return new DefValue<TDef, float>
            {
                Def = a.Def ?? b.Def,
                Value = (a - b.Value).Value
            };
        }

        //Integer
        public static DefValue<TDef, int> operator +(DefValue<TDef, TValue> a, int b)
        {
            var value1 = a.Value switch
            {
                float f1 => Mathf.RoundToInt(f1),
                int i1 => i1,
                _ => 0
            };

            return new DefValue<TDef, int>(a.Def, value1 + b);
        }

        public static DefValue<TDef, int> operator -(DefValue<TDef, TValue> a, int b)
        {
            return a + (-b);
        }

        public static DefValue<TDef, int> operator +(DefValue<TDef, TValue> a, DefValue<TDef, int> b)
        {
            return a + b.Value;
        }

        public static DefValue<TDef, int> operator -(DefValue<TDef, TValue> a, DefValue<TDef, int> b)
        {
            return a - b.Value;
        }

        public override string ToString()
        {
            return $"(({Def.GetType()}):{Def}, {Value})";
        }
    }
}
