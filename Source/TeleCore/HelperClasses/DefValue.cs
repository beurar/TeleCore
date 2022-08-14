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
    public class DefValueDef<T,V> where T : Def
    {
        public T def;
        public V value;

        public T Def => def;
        public V Value
        {
            get => value;
            set => this.value = value;
        }

        public bool IsValid => def != null && value is float or int;

        public static implicit operator DefValue<T, V>(DefValueDef<T, V> d) => new(d.Def, d.value);
        public static explicit operator DefValueDef<T, V>(DefValue<T, V> d) => new (d.Def, d.Value);

        public DefValueDef() { }

        public DefValueDef(T def, V value)
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
                value = ParseHelper.FromString<V>(array.Length > 1 ? array[1] : "1");
            }

            //InLined
            else
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(def)}", xmlRoot.Name);
                value = ParseHelper.FromString<V>(xmlRoot.FirstChild.Value);
            }
        }

        public override string ToString()
        {
            return $"{def?.LabelCap}: {value}";
        }

        public string ToStringPercent()
        {
            if (value is not float f) return ToString();
            return $"{def?.LabelCap}: ({f.ToStringPercent()})";
        }
    }

    public struct DefValue<T, V> where T : Def
    {
        public T Def { get; }
        public V Value { get; set; }
        
        public static explicit operator DefValue<T,V>(DefValueDef<T,V> defValue)
        {
            return new DefValue<T,V>(defValue);
        }

        public DefValue(DefValueDef<T,V> defValue)
        {
            this.Def = defValue.def;
            this.Value = defValue.value;
        }

        public DefValue(T def, V value)
        {
            this.Def = def;
            this.Value = value;
        }

        //Float
        public static DefValue<T, float> operator +(DefValue<T, V> a, float b)
        {
            var value1 = a.Value switch
            {
                float f1 => f1,
                int i1 => i1,
                _ => 0
            };

            return new DefValue<T, float>(a.Def, value1 + b);
        }

        public static DefValue<T, float> operator -(DefValue<T, V> a, float b)
        {
            var value1 = a.Value switch
            {
                float f1 => f1,
                int i1 => i1,
                _ => 0
            };

            return new DefValue<T, float>(a.Def, value1 - b);
        }

        public static DefValue<T,float> operator +(DefValue<T,V> a, DefValue<T,float> b)
        {
            return a + b.Value;
        }

        public static DefValue<T, float> operator -(DefValue<T, V> a, DefValue<T, float> b)
        {
            return a - b.Value;
        }

        //Integer
        public static DefValue<T, int> operator +(DefValue<T, V> a, int b)
        {
            var value1 = a.Value switch
            {
                float f1 => Mathf.RoundToInt(f1),
                int i1 => i1,
                _ => 0
            };

            return new DefValue<T, int>(a.Def, value1 + b);
        }

        public static DefValue<T, int> operator -(DefValue<T, V> a, int b)
        {
            return a + (-b);
        }

        public static DefValue<T, int> operator +(DefValue<T, V> a, DefValue<T, int> b)
        {
            return a + b.Value;
        }

        public static DefValue<T, int> operator -(DefValue<T, V> a, DefValue<T, int> b)
        {
            return a - b.Value;
        }

        public override string ToString()
        {
            return $"({Def}, {Value})";
        }
    }
}
