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
        public static Dictionary<SubThingGroupDef, Dictionary<SubThingCategory, List<ThingDef>>> Categorized = new();
        public static List<SubThingGroupDef> AllSubGroupDefs = new List<SubThingGroupDef>();


        public static Dictionary<SubThingGroupDef, Dictionary<SubThingCategory, List<Designator>>> ResolvedDesignators = new();

        static SubMenuThingDefList()
        {
            var list1 = DefDatabase<SubThingGroupDef>.AllDefs;
            var list2 = DefDatabase<SubThingCategory>.AllDefs;
            for (int i = 0; i < list1.Count(); i++)
            {
                SubThingGroupDef des = list1.ElementAt(i);
                var dict = new Dictionary<SubThingCategory, List<ThingDef>>();
                var designatorDict = new Dictionary<SubThingCategory, List<Designator>>();
                for (int j = 0; j < list2.Count(); j++)
                {
                    SubThingCategory cat = list2.ElementAt(j);
                    dict.Add(cat, new List<ThingDef>());
                    designatorDict.Add(cat, new List<Designator>());
                }
                Categorized.Add(des, dict);
                AllSubGroupDefs.Add(des);
                ResolvedDesignators.Add(des, designatorDict);
            }
        }

        //Discovery
        public static bool HasUnDiscovered(SubThingGroupDef faction)
        {
            return Categorized[faction].Any(d => HasUnDiscovered(faction, d.Key));
        }

        public static bool HasUnDiscovered(SubThingGroupDef faction, SubThingCategory category)
        {
            return Categorized[faction][category].Any(d => !ConStructionOptionDiscovered(d) && d.IsResearchFinished);
        }

        internal static bool ConStructionOptionDiscovered(ThingDef def)
        {
            return StaticData.WorldCompTele().discoveryTable.MenuOptionHasBeenSeen(def);
        }

        public static void Add(ThingDef def, TeleDefExtension extension)
        {
            //AllDefs.Add(def);
            var groupDef = extension.subMenuDesignation.groupDef;
            var category = extension.subMenuDesignation.category;
            if (groupDef == null || category == null)
            {
                TLog.Error($"Error at {def} in 'subMenuDesignation': {(groupDef == null ? "groupDef missing.": "")} {(category == null ? "category missing." : "")}");
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
