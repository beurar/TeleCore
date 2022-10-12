using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TeleCore
{
    internal static class SubMenuThingDefList
    {
        public static Dictionary<SubMenuGroupDef, Dictionary<SubMenuCategoryDef, List<ThingDef>>> Categorized = new();
        public static List<SubMenuGroupDef> AllSubGroupDefs = new List<SubMenuGroupDef>();


        public static Dictionary<SubMenuGroupDef, Dictionary<SubMenuCategoryDef, List<Designator>>> ResolvedDesignators = new();

        static SubMenuThingDefList()
        {
            var list1 = DefDatabase<SubMenuGroupDef>.AllDefs;
            var list2 = DefDatabase<SubMenuCategoryDef>.AllDefs;
            for (int i = 0; i < list1.Count(); i++)
            {
                SubMenuGroupDef des = list1.ElementAt(i);
                var dict = new Dictionary<SubMenuCategoryDef, List<ThingDef>>();
                var designatorDict = new Dictionary<SubMenuCategoryDef, List<Designator>>();
                for (int j = 0; j < list2.Count(); j++)
                {
                    SubMenuCategoryDef cat = list2.ElementAt(j);
                    dict.Add(cat, new List<ThingDef>());
                    designatorDict.Add(cat, new List<Designator>());
                }
                Categorized.Add(des, dict);
                AllSubGroupDefs.Add(des);
                ResolvedDesignators.Add(des, designatorDict);
            }
        }

        //Discovery
        public static bool HasUnDiscovered(SubMenuGroupDef group)
        {
            return Categorized[group].Any(d => HasUnDiscovered(group, d.Key));
        }

        public static bool IsActive(ThingDef def)
        {
            return def.IsResearchFinished;
        }

        public static bool HasUnDiscovered(SubMenuGroupDef group, SubMenuCategoryDef categoryDef)
        {
            return Categorized[group][categoryDef].Any(d => !ConstructionOptionDiscovered(d) && IsActive(d));
        }

        internal static bool ConstructionOptionDiscovered(ThingDef def)
        {
            return StaticData.WorldCompTele().discoveryTable.MenuOptionHasBeenSeen(def);
        }

        internal static void Discover_ConstructionOption(ThingDef def)
        {
            StaticData.WorldCompTele().discoveryTable.DiscoverInMenu(def);
        }

        public static void Add(ThingDef def, SubMenuExtension extension)
        {
            //AllDefs.Add(def);
            TLog.Message($"Adding SubMenu for {def}");
            var groupDef = extension.groupDef;
            var category = extension.category;
            if (groupDef == null || category == null)
            {
                TLog.Error($"Error at {def} in 'subMenuDesignation': [{groupDef}, {category}, {extension.hidden}] {(groupDef == null ? "groupDef missing.": "")} {(category == null ? "category missing." : "")}");
                return;
            }
            if (!Categorized[groupDef][category].Contains(def))
            {
                Categorized[groupDef][category].Add(def);
                ResolvedDesignators[groupDef][category].Add(new Designator_Build(def));
            }
            //if (!props.menuHidden)
            //Log.Error(props +  " should have menuHidden");
        }
    }
}
