using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse.Profile;

namespace TeleCore
{
    internal static class UpdatePatches
    {
        //
        [HarmonyPatch(typeof(MemoryUtility))]
        [HarmonyPatch(nameof(MemoryUtility.ClearAllMapsAndWorld))]
        internal static class MemoryUtility_ClearAllMapsAndWorldPatch
        {
            public static void Postfix()
            {
                StaticData.Notify_ClearingMapAndWorld();
            }
        }
    }
}
