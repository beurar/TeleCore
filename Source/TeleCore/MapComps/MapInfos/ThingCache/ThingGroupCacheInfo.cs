using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class ThingGroupCacheInfo : MapInformation
    {
        private readonly Dictionary<ThingGroupDef, List<Thing>> cachedThingsByGroup = new();
        private readonly Dictionary<ThingGroupDef, List<ThingWithComps>> cachedCompParentsByGroup = new();
        private readonly Dictionary<ThingGroupDef, List<ThingComp>> cachedCompsByGroup = new();

        public ThingGroupCacheInfo(Map map) : base(map)
        {
        }

        public List<Thing> ThingsOfGroup(ThingGroupDef group)
        {
            return cachedThingsByGroup.TryGetValue(group);
        }

        public List<ThingWithComps> GetCompParentsFromGroup(ThingGroupDef group)
        {
            if (group == null) return null;
            return cachedCompParentsByGroup.TryGetValue(group);
        }

        public List<ThingComp> GetCompsFromGroup(ThingGroupDef group)
        {
            return cachedCompsByGroup.TryGetValue(group);
        }

        //
        public void RegisterPart(ThingGroupDef groupDef, object obj)
        {
            if (groupDef == null) return;
            switch (obj)
            {
                case Thing thing:
                {
                    if (!cachedThingsByGroup.ContainsKey(groupDef))
                    {
                        cachedThingsByGroup.Add(groupDef, new List<Thing>());
                    }
                    cachedThingsByGroup[groupDef].Add(thing);

                    break;
                }
                case ThingComp comp:
                {
                    if (!cachedCompsByGroup.ContainsKey(groupDef))
                    {
                        cachedCompsByGroup.Add(groupDef, new List<ThingComp>());
                    }
                    cachedCompsByGroup[groupDef].Add(comp);

                    //Cache Parent
                    if (!cachedCompParentsByGroup.ContainsKey(groupDef))
                    {
                        cachedCompParentsByGroup.Add(groupDef, new List<ThingWithComps>());
                    }
                    cachedCompParentsByGroup[groupDef].Add(comp.parent);
                    break;
                }
            }
            
            //
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
                    cachedThingsByGroup[groupDef].Remove(thing);
                    break;
                }
                case ThingComp comp:
                {
                    cachedCompsByGroup[groupDef].Remove(comp);
                    cachedCompParentsByGroup[groupDef].Remove(comp.parent);
                    break;
                }
            }
            
            //
            if (groupDef.ParentGroup != null)
                DeregisterPart(groupDef.ParentGroup, obj);
        }
    }
}
