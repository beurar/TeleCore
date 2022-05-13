using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal static class UIPatches
    {
        [HarmonyPatch(typeof(MainMenuDrawer))]
        [HarmonyPatch(nameof(MainMenuDrawer.DoMainMenuControls))]
        public static class DoMainMenuControlsPatch
        {
            public static float addedHeight = 45f + 7f;
            public static List<ListableOption> OptionList;
            private static MethodInfo ListingOption = SymbolExtensions.GetMethodInfo(() => AdjustList(null));

            static void AdjustList(List<ListableOption> optList)
            {
                var label = "Options".Translate();
                var idx = optList.FirstIndexOf(opt => opt.label == label);
                if (idx > 0 && idx < optList.Count) optList.Insert(idx + 1, new ListableOption(StringCache.TeleTools, delegate ()
                {
                    Find.WindowStack.Add(new Dialog_ToolSelection());
                }, null));
                OptionList = optList;
            }

            static bool Prefix(ref Rect rect, bool anyMapFiles)
            {
                rect = new Rect(rect.x, rect.y, rect.width, rect.height + addedHeight);
                return true;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var m_DrawOptionListing = SymbolExtensions.GetMethodInfo(() => OptionListingUtility.DrawOptionListing(Rect.zero, null));

                var instructionsList = instructions.ToList();
                var patched = false;
                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var instruction = instructionsList[i];
                    if (i + 2 < instructionsList.Count)
                    {
                        var checkingIns = instructionsList[i + 2];
                        if (!patched && checkingIns != null && checkingIns.Calls(m_DrawOptionListing))
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc_2);
                            yield return new CodeInstruction(OpCodes.Call, ListingOption);
                            patched = true;
                        }
                    }
                    yield return instruction;
                }
            }
        }

        //Dialogs
        [HarmonyPatch(typeof(Dialog_BillConfig))]
        [HarmonyPatch("DoWindowContents")]
        public static class Dialog_BillConfigDoWindowContentsPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo methodFinder = AccessTools.Method(typeof(StringBuilder), nameof(StringBuilder.AppendLine));
                MethodInfo helper = AccessTools.Method(typeof(Dialog_BillConfigDoWindowContentsPatch), nameof(WriteNetworkCost));

                bool continuedToPop = false, finalPatched = false;
                int i = 0;
                foreach (var code in instructions)
                {
                    if (i < 2 && code.opcode == OpCodes.Callvirt && code.operand.Equals(methodFinder))
                    {
                        i++;
                    }

                    yield return code;
                    if (i != 2) continue;

                    if (!continuedToPop)
                    {
                        continuedToPop = true;
                        continue;
                    }

                    if (!finalPatched)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 34);
                        yield return new CodeInstruction(OpCodes.Call, helper);
                        finalPatched = true;
                    }
                }

            }

            private static void WriteNetworkCost(Dialog_BillConfig instance, StringBuilder stringBuilder)
            {
                if (instance.bill is NetworkBill_Production tBill)
                {
                    stringBuilder.AppendLine($"Network Cost:");
                    foreach (var cost in tBill.def.networkCost.Cost.SpecificCosts)
                    {
                        stringBuilder.AppendLine($" - {cost.valueDef.LabelCap.Colorize(cost.valueDef.valueColor)}: {cost.value}");
                    }

                    stringBuilder.AppendLine($"BaseShouldBeDone: {tBill.BaseShouldDo}");
                    stringBuilder.AppendLine($"ShouldBeDone: {tBill.ShouldDoNow()}");
                    stringBuilder.AppendLine($"CompTNW: {tBill.CompTNW is { IsPowered: true }}");
                    stringBuilder.AppendLine($"def.CanPay: {tBill.def.networkCost.CanPayWith(tBill.CompTNW)}");
                }
            }
        }
    }
}
