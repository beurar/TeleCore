using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore.Static;
using Verse;

namespace TeleCore
{
    public class TeleDefExtension : DefModExtension
    {
        //
        public List<ThingGroupDef> thingGroups = new List<ThingGroupDef>(){ThingGroupDefOf.All};

        public List<GraphicData> extraGraphics;

        public FXDefExtension graphics;
        public TurretDefExtension turret;
        public ProjectileDefExtension projectile;
    }
}
