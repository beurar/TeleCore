using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal static class ThingPatches
    {
        [HarmonyPatch(typeof(Thing))]
        [HarmonyPatch(nameof(Thing.SpawnSetup))]
        public static class SpawnSetupPatch
        {
            public static void Postfix(Thing __instance)
            {
                var TeleCore = __instance.MapHeld.TeleCore();

                //Register For DataBase
                TeleCore.Notify_ThingSpawned(__instance);
            }
        }

        [HarmonyPatch(typeof(Thing))]
        [HarmonyPatch(nameof(Thing.DeSpawn))]
        public static class DeSpawnPatch
        {
            public static bool Prefix(Thing __instance)
            {
                var TeleCore = __instance.MapHeld.TeleCore();

                //Register For DataBase
                TeleCore.Notify_DespawnedThing(__instance);
                return true;
            }
        }

        //Projectiles
        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch(nameof(Projectile.ArcHeightFactor), MethodType.Getter)]
        public static class ProjectileArcHeightFactorPatch
        {
            public static void Postfix(Projectile __instance, ref float __result)
            {
                if (__instance is IPatchedProjectile patchedProjectile)
                {
                    __result += patchedProjectile.ArcHeightFactorPostAdd;
                }
            }
        }

        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch(nameof(Projectile.CanHit))]
        public static class ProjectileCanHitPatch
        {
            public static void Postfix(Projectile __instance, Thing thing, ref bool __result)
            {
                if (__instance is IPatchedProjectile patchedProjectile)
                {
                    patchedProjectile.CanHitOverride(thing, ref __result);
                }
            }
        }

        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch(nameof(Projectile.Launch), typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef))]
        public static class ProjectileLaunchPatch
        {
            public static bool Prefix(Projectile __instance, Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
            {
                //
                if (__instance is IPatchedProjectile patchedProjectile)
                {
                    if (!patchedProjectile.PreLaunch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef))
                    {
                        return false;
                    }
                }
                return true;
            }

            public static void Postfix(Projectile __instance, Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
            {
                //
                if (__instance is IPatchedProjectile patchedProjectile)
                {
                    patchedProjectile.PostLaunch(ref __instance.origin, ref __instance.destination);
                }
            }
        }

        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch(nameof(Projectile.ImpactSomething))]
        public static class ProjectileImpactPatch
        {
            private static IntVec3 Position;
            private static Map Map;

            public static bool Prefix(Thing __instance)
            {
                Position = __instance.Position;
                Map = __instance.Map;

                //
                if (__instance is IPatchedProjectile patchedProj)
                {
                    return patchedProj.PreImpact();
                }

                return true;
            }

            public static void Postfix(Thing __instance)
            {
                //
                if (__instance is IPatchedProjectile patchedProj)
                {
                    patchedProj.PostImpact();
                }

                //
                if (__instance.def.HasTeleExtension(out var extension))
                {
                    if (extension.projectile != null)
                    {
                        extension.projectile.impactExplosion?.DoExplosion(Position, Map, __instance);
                        extension.projectile.impactFilth?.SpawnFilth(Position, Map);
                        extension.projectile.impactEffecter?.Spawn(Position, Map);
                    }
                }

                //
                Position = IntVec3.Invalid;
                Map = null;
            }
        }
    }
}
