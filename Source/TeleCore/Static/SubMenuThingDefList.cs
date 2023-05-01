using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TeleCore
{
    internal static class SubMenuThingDefList
    {
        public static Dictionary<SubMenuGroupDef, Dictionary<SubMenuCategoryDef, List<BuildableDef>>> Categorized = new();
        public static List<SubMenuGroupDef> AllSubGroupDefs = new List<SubMenuGroupDef>();


        public static Dictionary<SubMenuGroupDef, Dictionary<SubMenuCategoryDef, List<Designator>>> ResolvedDesignators = new();

        static SubMenuThingDefList()
        {
            var list1 = DefDatabase<SubMenuGroupDef>.AllDefs;
            var list2 = DefDatabase<SubMenuCategoryDef>.AllDefs;
            for (int i = 0; i < list1.Count(); i++)
            {
                SubMenuGroupDef des = list1.ElementAt(i);
                var dict = new Dictionary<SubMenuCategoryDef, List<BuildableDef>>();
                var designatorDict = new Dictionary<SubMenuCategoryDef, List<Designator>>();
                for (int j = 0; j < list2.Count(); j++)
                {
                    SubMenuCategoryDef cat = list2.ElementAt(j);
                    dict.Add(cat, new List<BuildableDef>());
                    designatorDict.Add(cat, new List<Designator>());
                }
                Categorized.Add(des, dict);
                AllSubGroupDefs.Add(des);
                ResolvedDesignators.Add(des, designatorDict);
            }
        }

        //Discovery
        public static bool IsActive(SubBuildMenuDef inMenu, BuildableDef def)
        {
            return DebugSettings.godMode || (inMenu.VisWorker?.IsAllowed(def) ?? true);
        }
        
        public static void Add(BuildableDef def, SubMenuExtension extension)
        {
            var groupDef = extension.groupDef;
            var category = extension.category;
            if (groupDef == null || category == null)
            {
                TLog.Error($"Error at {def} in 'subMenuDesignation': [{groupDef}, {category}, {extension.isDevOption}] {(groupDef == null ? "groupDef missing.": "")} {(category == null ? "category missing." : "")}");
                return;
            }
            if (!Categorized[groupDef][category].Contains(def))
            {
                Categorized[groupDef][category].Add(def);
                ResolvedDesignators[groupDef][category].Add(new Designator_Build(def));
            }
        }
    }
}
