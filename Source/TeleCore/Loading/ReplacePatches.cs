using HarmonyLib;
using RimWorld;
using TeleCore.Network.Bills;

namespace TeleCore.Loading;

internal static class ReplacePatches
{
    [HarmonyPatch(typeof(BillUtility))]
    [HarmonyPatch("MakeNewBill")]
    public static class MakeNewBillPatch
    {
        public static void Postfix(ref Bill __result)
        {
            if (__result.recipe is RecipeDef_Network {networkCost: { }} tRecipe)
            {
                Bill_Production_Network billProductionNetworkBill = new Bill_Production_Network(tRecipe);
                __result = billProductionNetworkBill;
            }
        }
    }
}