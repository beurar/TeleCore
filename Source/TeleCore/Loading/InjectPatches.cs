using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal static class InjectPatches
    {
        //This adds gizmos to the pawn
        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch("GetGizmos")]
        internal static class Pawn_GetGizmoPatch
        {
            private static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
            {
                List<Gizmo> gizmos = new List<Gizmo>(__result);

                foreach (var hediff in __instance.health.hediffSet.hediffs)
                {
                    if (hediff is HediffWithGizmos gizmoDiff)
                    {
                        gizmos.AddRange(gizmoDiff.GetGizmos());
                    }
                    var gizmoComp = hediff.TryGetComp<HediffComp_Gizmo>();
                    if (gizmoComp != null)
                        gizmos.AddRange(gizmoComp.GetGizmos());
                }
                __result = gizmos;
            }
        }

        //
        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("AddDraftedOrders")]
        public static class Pawn_AddDraftedOrdersPatch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                if (!pawn.PawnHasRangedHediffVerb()) return;
                foreach (LocalTargetInfo attackTarg in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true, null))
                {
                    Action rangedAct = HediffRangedUtility.GetRangedAttackAction(pawn, attackTarg, out string str);
                    string text = "FireAt".Translate(attackTarg.Thing.Label, attackTarg.Thing);
                    FloatMenuOption floatMenuOption = new FloatMenuOption("", null, MenuOptionPriority.High, null, attackTarg.Thing, 0f, null, null);
                    if (rangedAct == null)
                        text += ": " + str;
                    else
                    {
                        floatMenuOption.autoTakeable = (attackTarg.HasThing || attackTarg.Thing.HostileTo(Faction.OfPlayer));
                        floatMenuOption.autoTakeablePriority = 40f;
                        floatMenuOption.action = delegate ()
                        {
                            FleckMaker.Static(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, FleckDefOf.FeedbackShoot, 1f);
                            rangedAct();
                        };
                    }
                    floatMenuOption.Label = text;
                    opts.Add(floatMenuOption);
                }
            }
        }

        //
        [HarmonyPatch]
        public static class EditablePostLoadPatch
        {
            [HarmonyTargetMethods]
            public static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return AccessTools.Method(typeof(Def), "PostLoad");
                yield return AccessTools.Method(typeof(PawnKindDef), "PostLoad");
                yield return AccessTools.Method(typeof(ThingStyleDef), "PostLoad");
                yield return AccessTools.Method(typeof(BodyPartDef), "PostLoad");
                yield return AccessTools.Method(typeof(FactionDef), "PostLoad");
                yield return AccessTools.Method(typeof(ThingCategoryDef), "PostLoad");
                
                yield return AccessTools.Method(typeof(SongDef), "PostLoad");
                yield return AccessTools.Method(typeof(SkillDef), "PostLoad");
                yield return AccessTools.Method(typeof(AbilityDef), "PostLoad");
                yield return AccessTools.Method(typeof(MechWorkModeDef), "PostLoad");
            }

            public static void Postfix(Def __instance)
            {
                DefIDStack.RegisterNew(__instance);
            }
        }
    }
}
