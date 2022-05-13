using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TeleCore
{
    internal static class NetworkBillUtility
    {
        public static DefValue<NetworkValueDef>[] ConstructCustomCost(List<DefCount<CustomRecipeRatioDef>> list)
        {
            var allValues = list.SelectMany(t => t.Def.inputRatio.Select(r => new DefValue<NetworkValueDef>(r.def, r.value * t.Value)));
            var allDefs = allValues.Select(d => d.Def).Distinct().ToList();
            var tempCost = new DefValue<NetworkValueDef>[allDefs.Count];
            tempCost.Populate(allDefs.Select(d => new DefValue<NetworkValueDef>(d, 0)));
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
        }

        public static DefValue<NetworkValueDef>[] ConstructCustomCost(IDictionary<CustomRecipeRatioDef, int> requestedAmount)
        {
            var allValues = requestedAmount.SelectMany(t => t.Key.inputRatio.Select(r => new DefValue<NetworkValueDef>(r.def, r.value * t.Value)));
            var allDefs = allValues.Where(d => d.Value > 0).Select(d => d.Def).Distinct().ToList();
            var tempCost = new DefValue<NetworkValueDef>[allDefs.Count];
            tempCost.Populate(allDefs.Select(d => new DefValue<NetworkValueDef>(d, 0)));
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
        }

        public static string CostLabel(DefValue<NetworkValueDef>[] values)
        {
            if (values.NullOrEmpty()) return "N/A";
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
    }
}
