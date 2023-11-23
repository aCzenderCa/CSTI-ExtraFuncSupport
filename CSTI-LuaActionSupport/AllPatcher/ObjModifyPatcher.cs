using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

[HarmonyPatch]
public static class ObjModifyPatcher
{
    public static readonly Regex LuaDesc = new(@"^\#\#\#luaAction CardDescription\n(?<luaCode>[\s\S]*?)\n\#\#\#$");

    [HarmonyPostfix, HarmonyPatch(typeof(DismantleActionButton), nameof(DismantleActionButton.Setup))]
    public static void DismantleActionButton_PostSetup(DismantleActionButton __instance, int _Index,
        DismantleCardAction _Action,
        InGameCardBase _Card, bool _Highlighted, bool _StackVersion)
    {
        if (LuaDesc.Match(__instance.Text) is not {Success: true} match) return;
        var code = match.Groups["luaCode"].Value;
        var lua = InitRuntime(GameManager.Instance);
        lua["receive"] = new CardAccessBridge(_Card);
        ModData["Args__instance"] = __instance;
        ModData["Args__Index"] = _Index;
        ModData["Args__Action"] = _Action;
        ModData["Args__Highlighted"] = _Highlighted;
        ModData["Args__StackVersion"] = _StackVersion;
        var luaScriptRetValues = new LuaScriptRetValues();
        lua["Ret"] = luaScriptRetValues;
        try
        {
            string? ret;
            bool? show;
            bool? canUse;
            _ = lua.DoString(code, "LuaCodeActionName");
            if (luaScriptRetValues.CheckKey(nameof(ret), out ret))
            {
                __instance.Text = ret ?? __instance.Text;
            }

            if (luaScriptRetValues.CheckKey(nameof(show), out show))
            {
                __instance.gameObject.SetActive(show ?? __instance.gameObject.activeInHierarchy);
            }

            if (luaScriptRetValues.CheckKey(nameof(canUse), out canUse))
            {
                __instance.ConditionsValid = canUse ?? __instance.ConditionsValid;
                __instance.Interactable = canUse ?? __instance.Interactable;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.CardDescription))]
    public static void ModifyCardDescription(InGameCardBase __instance, bool _IgnoreLiquid, ref string __result)
    {
        var cardModelUniqueID = __instance.CardModel.UniqueID;
        if (LoadCurrentSlot(SimpleUniqueAccess.SaveKey) is DataNodeTableAccessBridge accessBridge
            && accessBridge[cardModelUniqueID] is DataNodeTableAccessBridge uniqueAccessBridge)
        {
            if (!__instance.ContainedLiquid || _IgnoreLiquid)
            {
                __result = uniqueAccessBridge[nameof(CardData.CardDescription)] as string ?? __result;
            }
            else if (__instance.ContainedLiquid &&
                     accessBridge.Table?.ContainsKey(__instance.ContainedLiquid.CardModel.UniqueID) is true &&
                     accessBridge[__instance.ContainedLiquid.CardModel.UniqueID] is DataNodeTableAccessBridge
                         liqTableAccessBridge)
            {
                __result = liqTableAccessBridge[nameof(CardData.CardDescription)] as string ?? __result;
            }
        }

        if (LuaDesc.Match(__result) is not {Success: true} match) return;
        var code = match.Groups["luaCode"].Value;
        var lua = InitRuntime(GameManager.Instance);
        lua["receive"] = new CardAccessBridge(__instance);
        var luaScriptRetValues = new LuaScriptRetValues();
        lua["Ret"] = luaScriptRetValues;
        try
        {
            string? ret;
            _ = lua.DoString(code, "LuaCodeDesc");
            if (luaScriptRetValues.CheckKey(nameof(ret), out ret))
            {
                __result = ret ?? __result;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    [HarmonyPatch(typeof(GameManager))]
    public static class Patch_GameManager_AddCard
    {
        [HarmonyTargetMethod]
        public static MethodBase FindTargetAddCard()
        {
            return AccessTools.GetDeclaredMethods(typeof(GameManager)).First(info =>
                info.Name == nameof(GameManager.AddCard) && info.GetParameters().Length > 16);
        }

        [HarmonyPostfix]
        public static void MoniRawAddCard(ref IEnumerator __result)
        {
            if (MoniEnum.OnMoniAddCard)
            {
                __result = MoniEnum.MoniFunc(__result);
            }
        }
    }
}