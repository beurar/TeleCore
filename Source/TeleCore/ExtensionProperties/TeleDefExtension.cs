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
        public ThingGroupDef thingGroup = ThingGroupDefOf.All;

        public FXDefExtension graphics;
        public TurretDefExtension turret;
        public ProjectileDefExtension projectile;
    }
}
