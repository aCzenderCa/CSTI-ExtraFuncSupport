using System;
using System.Text.RegularExpressions;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.CardActionPatcher;

namespace CSTI_LuaActionSupport.AllPatcher
{
    [HarmonyPatch]
    public static class ObjModifyPatcher
    {
        public static readonly Regex LuaDesc = new(@"\#\#\#luaAction CardDescription\n(?<luaCode>[\s\S]*?)\n\#\#\#");

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
            }

            if (LuaDesc.Match(__result) is {Success: true} match)
            {
                var value = match.Groups["luaCode"].Value;
                var lua = InitRuntime(GameManager.Instance);
                lua["receive"] = new CardAccessBridge(__instance);
                var luaScriptRetValues = new LuaScriptRetValues();
                lua["Ret"] = luaScriptRetValues;
                try
                {
                    string? ret;
                    _ = lua.DoString(value, "LuaCodeDesc");
                    if (luaScriptRetValues.CheckKey(nameof(ret), out ret))
                    {
                        __result = ret ?? __result;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
}