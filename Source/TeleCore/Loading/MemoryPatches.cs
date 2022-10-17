using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TeleCore.Loading;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace TeleCore;

internal static class MemoryPatches
{
    [HarmonyPatch(typeof(MemoryUtility))]
    [HarmonyPatch(nameof(MemoryUtility.ClearAllMapsAndWorld))]
    internal static class MemoryUtility_ClearAllMapsAndWorldPatch
    {
        public static void Postfix()
        {
            StaticData.Notify_ClearingMapAndWorld();

            //
            UnloadUtility.MemoryUnloadEvent?.Invoke();

            if (UnloadUtility.MemoryUnloadEventThreadSafe != null)
            {
                LongEventHandler.ExecuteWhenFinished(UnloadUtility.MemoryUnloadEventThreadSafe);
            }
        }
    }
}

