using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TeleCore;

internal static class NetworkBillUtility
{
    public static DefValueStack<NetworkValueDef> ConstructCustomCostStack(List<DefValueLoadable<CustomRecipeRatioDef>> list, bool isByProduct = false)
    {
        DefValueStack<NetworkValueDef> stack = new DefValueStack<NetworkValueDef>();
        foreach (var defIntRef in list)
        {
            if (isByProduct)
            {
                foreach (var ratio in defIntRef.Def.byProducts)
                {
                    stack += (DefValue<NetworkValueDef>) ratio * defIntRef.Value.AsT0;
                }
                continue;
            }

            foreach (var ratio in defIntRef.Def.inputRatio)
            {
                stack += (DefValue<NetworkValueDef>) ratio * defIntRef.Value.AsT0;
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
                    stack += (DefValue<NetworkValueDef>) ratio * defIntRef.Value;
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