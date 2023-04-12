using HarmonyLib;
using TeleCore.Data.Events;
using TeleCore.Static;
using Verse;

namespace TeleCore.Loading;

public static class EventInjectPatches
{
    [HarmonyPatch(typeof(Hediff))]
    [HarmonyPatch(nameof(Hediff.PostAdd))]
    public static class ProjectileArcHeightFactorPatch
    {
        public static void Postfix(Hediff __instance, DamageInfo? dinfo)
        {
            GlobalEventHandler.OnPawnHediffChanged(new PawnHediffChangedEventArgs(__instance, dinfo));
        }
    }
}