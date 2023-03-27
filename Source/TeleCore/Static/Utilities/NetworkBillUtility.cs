using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TeleCore
{
    internal static class NetworkBillUtility
    {
        public static DefValueStack<NetworkValueDef> ConstructCustomCostStack(List<DefIntRef<CustomRecipeRatioDef>> list, bool isByProduct = false)
        {
            DefValueStack<NetworkValueDef> stack = new DefValueStack<NetworkValueDef>();
            foreach (var defIntRef in list)
            {
                if (isByProduct)
                {
                    foreach (var ratio in defIntRef.Def.byProducts)
                    {
                        stack += (DefFloat<NetworkValueDef>) ratio * defIntRef.Value;
                    }
                    continue;
                }

                foreach (var ratio in defIntRef.Def.inputRatio)
                {
                    stack += (DefFloat<NetworkValueDef>) ratio * defIntRef.Value;
                }
            }
            return stack;
        }

        public static DefValueStack<NetworkValueDef> ConstructCustomCostStack(IDictionary<CustomRecipeRatioDef, int> requestedAmount, bool isByProduct = false)
        {
            DefValueStack<NetworkValueDef> stack = new DefValueStack<NetworkValueDef>();
            foreach (var defIntRef in requestedAmount)
            {
                if (isByProduct)
                {
                    foreach (var ratio in defIntRef.Key.byProducts)
                    {
                        stack += (DefFloat<NetworkValueDef>) ratio * defIntRef.Value;
                    }
                    continue;
                }
                
                foreach (var ratio in defIntRef.Key.inputRatio)
                {
                    stack += (DefFloat<NetworkValueDef>) ratio * defIntRef.Value;
                }
            }
            return stack;
        }
        
        /*public static List<DefFloat<NetworkValueDef>> ConstructCustomCost(List<DefIntRef<CustomRecipeRatioDef>> list)
        {
            var allValues = list.SelectMany(t => t.Def.inputRatio.
                Select(r => new DefFloat<NetworkValueDef>(r.Def, r.Value * t.Value)));
            //var allDefs = allValues.Select(d => d.Def).Distinct().ToList();
            //var tempCost = new List<DefFloat<NetworkValueDef>>(); //new DefFloat<NetworkValueDef>[allDefs.Count];

            DefValueStack<NetworkValueDef> stack = new DefValueStack<NetworkValueDef>();
            foreach (var def in allValues)
            {
                stack += def;
            }
            
            tempCost.Populate(allDefs.Select(d => new DefFloat<NetworkValueDef>(d, 0)));
            foreach (var value in allValues)
            {
                for (var i = 0; i < tempCost.Count; i++)
                {
                    var defValue = tempCost[i];
                    if (defValue.Def == value.Def)
                    {
                        tempCost[i].Value += value.Value;
                    }
                }
            }
            return stack.Values.ToList();
        }

        public static DefFloat<NetworkValueDef>[] ConstructCustomCost(IDictionary<CustomRecipeRatioDef, int> requestedAmount)
        {
            var allValues = requestedAmount.SelectMany(t =>
                t.Key.inputRatio.Select(r => new DefFloat<NetworkValueDef>(r.Def, r.Value * t.Value)));
            var allDefs = allValues.Where(d => d.Value > 0).Select(d => d.Def).Distinct().ToList();
            var tempCost = new DefFloat<NetworkValueDef>[allDefs.Count];
            tempCost.Populate(allDefs.Select(d => new DefFloat<NetworkValueDef>(d, 0)));
            foreach (var value in allValues)
            {
                for (var i = 0; i < tempCost.Length; i++)
                {
                    var defValue = tempCost[i];
                    if (defValue.Def == value.Def)
                    {
                        tempCost[i].Value += value.Value;
                    }
                }
            }

            return tempCost;
        }*/

        public static string CostLabel(DefValueStack<NetworkValueDef> values)
        {
            if (values.Empty) return "N/A";
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < values.Length; i++)
            {
                var input = values[i];
                sb.Append($"{input.Value}{input.Def.labelShort.Colorize(input.Def.valueColor)}");
                if (i + 1 < values.Length)
                    sb.Append(" ");
            }

            sb.Append(")");
            return sb.ToString();
        }
        
        public static string CostLabel(List<DefFloat<NetworkValueDef>> values)
        {
            if (values.NullOrEmpty()) return "N/A";
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < values.Count; i++)
            {
                var input = values[i];
                sb.Append($"{input.Value}{input.Def.labelShort.Colorize(input.Def.valueColor)}");
                if (i + 1 < values.Count)
                    sb.Append(" ");
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}
