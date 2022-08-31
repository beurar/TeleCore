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
    public class ListableOption_Tele : ListableOption
    {
        public ListableOption_Tele(string label, Action action, string uiHighlightTag = null) : base(label, action, uiHighlightTag) { }

        public override float DrawOption(Vector2 pos, float width)
        {
            var b = Text.CalcHeight(label, width);
            var num = Mathf.Max(minHeight, b);
            var rect = new Rect(pos.x, pos.y, width, num);

            Texture2D atlas = TeleContent.ButtonBGAtlas;
            if (Mouse.IsOver(rect))
            {
                atlas = TeleContent.ButtonBGAtlasMouseover;
                if (Input.GetMouseButton(0))
                {
                    atlas = TeleContent.ButtonBGAtlasClick;
                }
            }
            Widgets.DrawAtlas(rect, atlas);
            
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = default;


            if (Widgets.ButtonInvisible(rect)) 
                action();

            if (uiHighlightTag != null) 
                UIHighlighter.HighlightOpportunity(rect, uiHighlightTag);
            return num;
        }
    }

    internal static class UIPatches
    {
        [HarmonyPatch(typeof(MainTabWindow_Architect))]
        [HarmonyPatch(nameof(MainTabWindow_Architect.ClickedCategory))]
        internal static class ClickedCategoryPatch
        {
            static void Postfix(ArchitectCategoryTab Pan)
            {
                var subMenuDes = Pan.def.AllResolvedDesignators.Find(d => d is Designator_SubBuildMenu);
                if (subMenuDes is Designator_SubBuildMenu subMenu)
                {
                    subMenu.Toggle_Menu();
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuDrawer))]
        [HarmonyPatch(nameof(MainMenuDrawer.DoMainMenuControls))]
        internal static class DoMainMenuControlsPatch
        {
            private static float addedHeight = 45f + 7f;
            private static List<ListableOption> OptionList;
            private static MethodInfo ListingOption = SymbolExtensions.GetMethodInfo(() => AdjustList(null));

            static void AdjustList(List<ListableOption> optList)
            {
                var label = "Options".Translate();
                var idx = optList.FirstIndexOf(opt => opt.label == label);
                if (idx > 0 && idx < optList.Count) optList.Insert(idx + 1, new ListableOption_Tele(StringCache.TeleTools, delegate ()
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
        internal static class Dialog_BillConfigDoWindowContentsPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo methodFinder = AccessTools.Method(typeof(StringBuilder), nameof(StringBuilder.AppendLine));
                MethodInfo helper = AccessTools.Method(typeof(Dialog_BillConfigDoWindowContentsPatch),
                    nameof(WriteNetworkCost));

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

            static void WriteNetworkCost(Dialog_BillConfig instance, StringBuilder stringBuilder)
            {
                if (instance.bill is Bill_Production_Network tBill)
                {
                    stringBuilder.AppendLine($"Network Cost:");
                    foreach (var cost in tBill.def.networkCost.Cost.SpecificCosts)
                    {
                        stringBuilder.AppendLine(
                            $" - {cost.valueDef.LabelCap.Colorize(cost.valueDef.valueColor)}: {cost.value}");
                    }

                    stringBuilder.AppendLine($"BaseShouldBeDone: {tBill.BaseShouldDo}");
                    stringBuilder.AppendLine($"ShouldBeDone: {tBill.ShouldDoNow()}");
                    stringBuilder.AppendLine($"CompTNW: {tBill.CompTNW is {IsPowered: true}}");
                    stringBuilder.AppendLine($"def.CanPay: {tBill.def.networkCost.CanPayWith(tBill.CompTNW)}");
                }
            }
        }

        [HarmonyPatch(typeof(PlaySettings))]
        [HarmonyPatch(nameof(PlaySettings.DoPlaySettingsGlobalControls))]
        public static class PlaySettingsPatch
        {
            public static void Postfix(WidgetRow row, bool worldView)
            {
                if (worldView)
                {
                    if (row.ButtonIcon(TeleContent.AddKeyFrame))
                    {
                        Find.WindowStack.Add(DefDatabase<DevToolDef>.GetNamed("ModuleVisualizerDef").GetWindow);
                    }
                }

                if (worldView || row == null) return;

            }
        }

    }
}
