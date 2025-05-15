using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TeleCore.Data.Events;
using TeleCore.Events;
using UnityEngine;
using Verse;

namespace TeleCore;

internal static class VerbPatches
{
    #region Verb Attacher / Watcher

    [HarmonyPatch(typeof(VerbTracker), nameof(VerbTracker.InitVerb))]
    internal static class VerbTracker_InitVerb_Patch
    {
        private static void Postfix(Verb verb, VerbTracker __instance)
        {
            VerbWatcher.Notify_NewVerb(verb, __instance);
        }
    }
    
    // ## WarmUp ##
    [HarmonyPatch(typeof(Verb), nameof(Verb.WarmupComplete))]
    internal static class Verb_WarmupComplete_Patch
    {
        private static bool Prefix(Verb __instance)
        {
            VerbWatcher.GetAttacher(__instance)?.Notify_WarmupComplete();
            return true;
        }
    }

    // ## LaunchProjectile ##

    [HarmonyPatch(typeof(Verb_LaunchProjectile), nameof(Verb_LaunchProjectile.TryCastShot))]
    internal static class VerbLaunchProjectile_TryCastShot_Patch
    {
        private static readonly ConditionalWeakTable<Verb_LaunchProjectile, LaunchContext> _contexts = new();

        private class LaunchContext
        {
            public TeleVerbAttacher Attacher;
            public Projectile Projectile;
        }

        private static readonly MethodInfo ProjectileLaunch = AccessTools.Method(typeof(Projectile), nameof(Projectile.Launch),
            new[]
            {
                typeof(Thing),
                typeof(Vector3),
                typeof(LocalTargetInfo),
                typeof(LocalTargetInfo),
                typeof(ProjectileHitFlags),
                typeof(bool),
                typeof(Thing),
                typeof(ThingDef)
            });

        private static readonly MethodInfo StoreProjectileMethod = AccessTools.Method(
            typeof(VerbLaunchProjectile_TryCastShot_Patch),
            nameof(StoreProjectile));

        private static readonly MethodInfo TransformCastOriginMethod = AccessTools.Method(
            typeof(VerbLaunchProjectile_TryCastShot_Patch),
            nameof(TransformCastOrigin));

        [HarmonyPrefix]
        internal static void Prefix(Verb_LaunchProjectile __instance)
        {
            var context = new LaunchContext
            {
                Attacher = VerbWatcher.GetAttacher(__instance)
            };
            _contexts.Remove(__instance); // Clean any stale data
            _contexts.Add(__instance, context);
        }

        [HarmonyPostfix]
        internal static void Postfix(Verb_LaunchProjectile __instance)
        {
            if (!_contexts.TryGetValue(__instance, out var ctx))
                return;

            if (ctx.Projectile != null)
                GlobalEventHandler.OnProjectileLaunched(new ProjectileLaunchedArgs(ctx.Projectile));

            ctx.Attacher?.Notify_ProjectileLaunched(ctx.Projectile);
            _contexts.Remove(__instance);
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                // Inject TransformCastOrigin call
                if (code.opcode == OpCodes.Stloc_S && code.operand is LocalBuilder lb && lb.LocalIndex == 6)
                {
                    // Insert: call TransformCastOrigin(vector3, __instance)
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // __instance
                    yield return new CodeInstruction(OpCodes.Call, TransformCastOriginMethod); // Transforms with attacher
                }

                // After call to Projectile.Launch, store the launched projectile
                if (code.Calls(ProjectileLaunch))
                {
                    yield return code; // original call
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // __instance

                    // Projectile is typically stored in local index 7 after launch
                    // This must be verified in ILSpy; if incorrect, adjust.
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 7); // <- confirm this is the right local for the version, works in 1.5!
                    yield return new CodeInstruction(OpCodes.Call, StoreProjectileMethod);
                    continue;
                }

                yield return code;
            }
        }

        // Stores the launched projectile into the context
        internal static void StoreProjectile(Verb_LaunchProjectile verb, Projectile projectile)
        {
            if (_contexts.TryGetValue(verb, out var ctx))
                ctx.Projectile = projectile;
        }

        // Transforms cast origin if attacher is present
        internal static Vector3 TransformCastOrigin(Vector3 original, Verb_LaunchProjectile verb)
        {
            return _contexts.TryGetValue(verb, out var ctx)
                ? ctx.Attacher?.GetLaunchPosition(original) ?? original
                : original;
        }
    }

    #endregion

    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    internal static class PawnRenderer_DrawEquipmentAndApparelExtrasPatch
    {
        private static void Postfix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            if (pawn?.CurrentEffectiveVerb is Verb_Tele teleVerb) 
                teleVerb.DrawVerb(drawPos);
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
            if (!VerbWatcher.HasAttacher(verb)) return true;
            var attacher = VerbWatcher.GetAttacher(verb);
            __result = attacher?.Projectile ?? __result;
            return false;
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