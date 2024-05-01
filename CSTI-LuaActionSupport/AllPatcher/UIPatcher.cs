using CSTI_LuaActionSupport.DataStruct;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;

namespace CSTI_LuaActionSupport.AllPatcher;

public static class UIPatcher
{
    [HarmonyPostfix,
     HarmonyPatch(typeof(BlueprintConstructionPopup), nameof(BlueprintConstructionPopup.SetupActions), []),
    ]
    public static void BlueprintConstructionPopup_Setup(BlueprintConstructionPopup __instance)
    {
        if (__instance.CurrentCard != null && new CardAccessBridge(__instance.CurrentCard) is {Data: not null} bridge &&
            bridge.Data[nameof(ActionEffectPack.bpDropDisableCancel)] is true)
        {
            __instance.DeconstructButton.ConditionsValid = false;
        }
    }
}