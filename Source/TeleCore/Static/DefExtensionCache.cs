using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Verse;

namespace TeleCore
{
    public static class DefExtensionCache
    {
        private static readonly Dictionary<Def, TeleDefExtension> ExtensionByDef = new();

        //
        public static TeleDefExtension Tele(this Def def)
        {
            return ExtensionByDef.TryGetValue(def, out var extension) ? extension : null;
        }

        //
        public static bool HasTeleExtension(this Def def, out TeleDefExtension extension)
        {
            return ExtensionByDef.TryGetValue(def, out extension);
        }

        public static bool HasFXExtension(this Def def, out FXDefExtension extension)
        {
            return (extension = (ExtensionByDef.TryGetValue(def, out var tele) ? tele.graphics : null)) != null;
        }

        public static bool HasTurretExtension(this Def def, out FXDefExtension extension)
        {
            return (extension = (ExtensionByDef.TryGetValue(def, out var tele) ? tele.graphics : null)) != null;
        }

        //
        internal static void TryRegister(Def def)
        {
            var extension = def.GetModExtension<TeleDefExtension>();
            if (extension != null)
            {
                ExtensionByDef.Add(def, extension);
            }
        }
    }
}
