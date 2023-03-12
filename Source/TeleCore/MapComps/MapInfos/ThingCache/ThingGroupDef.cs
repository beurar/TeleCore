using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class ThingGroupDef : Def
{
    private ThingGroupDef parentGroup;
    public List<ThingGroupDef> subGroups;

    public ThingGroupDef ParentGroup => parentGroup;

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        foreach (var groupDef in DefDatabase<ThingGroupDef>.AllDefs)
        {
            if (parentGroup != null)
            {
                if (groupDef == parentGroup)
                {
                    groupDef.subGroups ??= new List<ThingGroupDef>();
                    groupDef.subGroups.Add(this);
                    break;
                }
                continue;
            }
            
            //
            if (groupDef.subGroups is null) continue;
            if (groupDef.subGroups.Contains(this))
            {
                parentGroup = groupDef;
                break;
            }
        }
    }
}

