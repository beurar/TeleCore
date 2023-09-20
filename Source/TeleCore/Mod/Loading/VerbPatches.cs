using HarmonyLib;
using Verse;

namespace TeleCore;

internal static class VerbPatches
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipment))]
    internal static class PawnRenderer_DrawEquipmentPatch
    {
        private static void Postfix(PawnRenderer __instance)
        {
            if (__instance.pawn?.CurrentEffectiveVerb is Verb_Tele teleVerb) 
                teleVerb.DrawVerb();
        }
    }

    [HarmonyPatch(typeof(Verb), nameof(Verb.VerbTick))]
    internal static class Verb_VerbTickPatch
    {
        private static bool Prefix(Verb __instance)
        {
            if (__instance is Verb_Tele teleVerb) teleVerb.PreVerbTick();
            return true;
        }

        private static void Postfix(Verb __instance)
        {
            if (__instance is Verb_Tele teleVerb) teleVerb.PostVerbTick();
        }
    }

    [HarmonyPatch(typeof(VerbTracker), nameof(VerbTracker.CreateVerbTargetCommand))]
    internal static class CreateVerbTargetCommandPatch
    {
        private static void Postfix(Command_VerbTarget __result, Verb verb)
        {
            if (!verb.Available())
                __result.Disable("Not available...");
        }
    }

    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.LaunchesProjectile), MethodType.Getter)]
    internal static class VerbPropertiesLaunchesProjectilePatch
    {
        private static bool Prefix(VerbProperties __instance, ref bool __result)
        {
            if (__instance is VerbProperties_Extended teleProps)
            {
                __result = teleProps.isProjectile;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(VerbUtility), nameof(VerbUtility.GetProjectile))]
    internal static class VerbUtilityGetProjectilePatch
    {
        private static bool Prefix(Verb verb, ref ThingDef __result)
        {
            if (verb is Verb_Tele verbTele)
            {
                __result = verbTele.Projectile;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(VerbUtility), nameof(VerbUtility.GetDamageDef))]
    internal static class VerbUtilityGetDamageDefPatch
    {
        private static bool Prefix(Verb verb, ref DamageDef __result)
        {
            if (verb is Verb_Tele verbTele)
            {
                __result = verbTele.DamageDef;
                return false;
            }

            return true;
        }
    }
}