using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public static class DefExtensionCache
    {
        private static readonly Dictionary<ThingDef, TeleDefExtension> ExtensionByDef = new();

        //
        public static TeleDefExtension Tele(this ThingDef def)
        {
            return ExtensionByDef.TryGetValue(def, out var extension) ? extension : null;
        }

        public static bool HasFXExtension(this ThingDef def, out FXDefExtension extension)
        {
            return (extension = (ExtensionByDef.TryGetValue(def, out var tele) ? tele.graphics : null)) != null;
        }

        public static bool HasTeleExtension(this ThingDef def, out TeleDefExtension extension)
        {
            return ExtensionByDef.TryGetValue(def, out extension);
        }

        internal static void TryRegister(ThingDef def)
        {
            var extension = def.GetModExtension<TeleDefExtension>();
            if (extension != null)
            {
                ExtensionByDef.Add(def, extension);
            }
        }
    }
}
