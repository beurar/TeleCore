using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class TeleCoreSettings : ModSettings
    {
        private bool enableProjectileGraphicRandomFix = false;

        //
        public bool ProjectileGraphicRandomFix => enableProjectileGraphicRandomFix;

        //
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableProjectileGraphicRandomFix, "enableProjectileGraphicRandomFix");
        }
    }
}
