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
        private static readonly Dictionary<string, List<CustomRecipeRatioDef>> _ratiosByTag = new();
        private static readonly Dictionary<string, List<CustomRecipePresetDef>> _presetsByTag = new();

        public static List<CustomRecipeRatioDef> TryGetRatiosOfTag(string tag)
        {
            if (_ratiosByTag.TryGetValue(tag, out var values))
            {
                return values;
            }
            TLog.Warning($"No {nameof(CustomRecipeRatioDef)} exists with tag '{tag}'");
            return null;
        }

        public static List<CustomRecipePresetDef> TryGetPresetsOfTag(string tag)
        {
            if (_presetsByTag.TryGetValue(tag, out var values))
            {
                return values;
            }
            TLog.Warning($"No {nameof(CustomRecipePresetDef)} exists with tag '{tag}'");
            return null;
        }

        public static void TryRegister(Def def)
        {
            if (def is CustomRecipeRatioDef {tags: { }} ratioDef)
            {
                foreach (var ratioDefTag in ratioDef.tags)
                {
                    if (!_ratiosByTag.ContainsKey(ratioDefTag))
                    {
                        _ratiosByTag.Add(ratioDefTag, new List<CustomRecipeRatioDef>() { ratioDef });
                        return;
                    }
                    _ratiosByTag[ratioDefTag].Add(ratioDef);
                }
            }

            if (def is CustomRecipePresetDef {tags: { }} presetDef)
            {
                foreach (var presetDefTag in presetDef.tags)
                {
                    if (!_presetsByTag.ContainsKey(presetDefTag))
                    {
                        _presetsByTag.Add(presetDefTag, new List<CustomRecipePresetDef>() { presetDef });
                        return;
                    }
                    _presetsByTag[presetDefTag].Add(presetDef);
                }
            }
        }
    }
}
