using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Verse;

namespace TeleCore
{
    internal static class CustomNetworkRecipeReferences
    {
        private static Dictionary<string, List<CustomRecipeRatioDef>> RatiosByTag = new();
        private static Dictionary<string, List<CustomRecipePresetDef>> PresetsByTag = new();

        public static List<CustomRecipeRatioDef> TryGetRatiosOfTag(string tag)
        {
            if (RatiosByTag.TryGetValue(tag, out var values))
            {
                return values;
            }
            TLog.Warning($"No {nameof(CustomRecipeRatioDef)} exists with tag '{tag}'");
            return null;
        }

        public static List<CustomRecipePresetDef> TryGetPresetsOfTag(string tag)
        {
            if (PresetsByTag.TryGetValue(tag, out var values))
            {
                return values;
            }
            TLog.Warning($"No {nameof(CustomRecipePresetDef)} exists with tag '{tag}'");
            return null;
        }

        public static void TryRegister(Def def)
        {
            if (def is CustomRecipeRatioDef ratioDef && ratioDef.tags != null)
            {
                foreach (var ratioDefTag in ratioDef.tags)
                {
                    if (!RatiosByTag.ContainsKey(ratioDefTag))
                    {
                        RatiosByTag.Add(ratioDefTag, new List<CustomRecipeRatioDef>() { ratioDef });
                        return;
                    }
                    RatiosByTag[ratioDefTag].Add(ratioDef);
                }
            }

            if (def is CustomRecipePresetDef presetDef && presetDef.tags != null)
            {
                foreach (var presetDefTag in presetDef.tags)
                {
                    if (!PresetsByTag.ContainsKey(presetDefTag))
                    {
                        PresetsByTag.Add(presetDefTag, new List<CustomRecipePresetDef>() { presetDef });
                        return;
                    }
                    PresetsByTag[presetDefTag].Add(presetDef);
                }
            }
        }
    }
}
