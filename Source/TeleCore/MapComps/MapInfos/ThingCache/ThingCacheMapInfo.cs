using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class ThingCacheMapInfo : MapInformation
    {
        public Dictionary<ThingGroupDef, List<Thing>> CachedThingsByGroup = new();
        public Dictionary<ThingGroupDef, List<ThingWithComps>> CachedCompParentsByGroup = new();
        public Dictionary<ThingGroupDef, List<ThingComp>> CachedCompsByGroup = new();

        public ThingCacheMapInfo(Map map) : base(map)
        {
        }

        public List<Thing> GetThingsFromGroup(ThingGroupDef group)
        {
            return CachedThingsByGroup.TryGetValue(group);
        }

        public List<ThingWithComps> GetCompParentsFromGroup(ThingGroupDef group)
        {
            if (group == null) return null;
            return CachedCompParentsByGroup.TryGetValue(group);
        }

        public List<ThingComp> GetCompsFromGroup(ThingGroupDef group)
        {
            return CachedCompsByGroup.TryGetValue(group);
        }

        //
        public void RegisterPart(ThingGroupDef groupDef, object obj)
        {
            if (groupDef == null) return;
            switch (obj)
            {
                case Thing thing:
                {
                    if (!CachedThingsByGroup.ContainsKey(groupDef))
                    {
                        CachedThingsByGroup.Add(groupDef, new List<Thing>());
                    }
                    CachedThingsByGroup[groupDef].Add(thing);

                    break;
                }
                case ThingComp comp:
                {
                    if (!CachedCompsByGroup.ContainsKey(groupDef))
                    {
                        CachedCompsByGroup.Add(groupDef, new List<ThingComp>());
                    }
                    CachedCompsByGroup[groupDef].Add(comp);

                    //Cache Parent
                    if (!CachedCompParentsByGroup.ContainsKey(groupDef))
                    {
                        CachedCompParentsByGroup.Add(groupDef, new List<ThingWithComps>());
                    }
                    CachedCompParentsByGroup[groupDef].Add(comp.parent);
                    break;
                }
            }
            if (groupDef.ParentGroup != null)
                RegisterPart(groupDef.ParentGroup, obj);
        }

        public void DeregisterPart(ThingGroupDef groupDef, object obj)
        {
            if (groupDef == null) return;
            switch (obj)
            {
                case Thing thing:
                {
                    CachedThingsByGroup[groupDef].Remove(thing);
                    break;
                }
                case ThingComp comp:
                {
                    CachedCompsByGroup[groupDef].Remove(comp);
                    CachedCompParentsByGroup[groupDef].Remove(comp.parent);
                    break;
                }
            }
            if (groupDef.ParentGroup != null)
                DeregisterPart(groupDef.ParentGroup, obj);
        }
    }
}
