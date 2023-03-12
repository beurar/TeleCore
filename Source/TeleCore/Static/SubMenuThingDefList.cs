using System.Collections.Generic;
using System.Linq;
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

        //Favorited
        public static bool IsFavorited(ThingDef def)
        {
            return TFind.UIProperties.MenuOptionIsFavorited(def);
        }

        public static bool ToggleFavorite(ThingDef def)
        {
            TFind.UIProperties.ToggleMenuOptionFavorite(def);
            return IsFavorited(def);
        }

        //Discovery
        public static bool HasUnDiscovered(SubBuildMenuDef inMenu, SubMenuGroupDef group)
        {
            return Categorized[group].Any(d => HasUnDiscovered(inMenu, group, d.Key));
        }

        public static bool IsActive(SubBuildMenuDef inMenu, ThingDef def)
        {
            return DebugSettings.godMode || (inMenu.AllowWorker?.IsAllowed(def) ?? true);
        }

        public static bool HasUnDiscovered(SubBuildMenuDef inMenu, SubMenuGroupDef group, SubMenuCategoryDef categoryDef)
        {
            return Categorized[group][categoryDef].Any(d => !ConstructionOptionDiscovered(d) && IsActive(inMenu, d));
        }

        internal static bool ConstructionOptionDiscovered(ThingDef def)
        {
            return TFind.Discoveries.MenuOptionHasBeenSeen(def);
        }

        internal static void Discover_ConstructionOption(ThingDef def)
        {
            TFind.Discoveries.DiscoverInMenu(def);
        }

        public static void Add(ThingDef def, SubMenuExtension extension)
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
