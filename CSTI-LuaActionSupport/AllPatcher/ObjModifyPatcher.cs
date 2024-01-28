using System;
using System.Linq;
using System.Text.RegularExpressions;
using CSTI_LuaActionSupport.DataStruct;
using CSTI_LuaActionSupport.LuaBuilder;
using CSTI_LuaActionSupport.LuaBuilder.CardDataModels;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using NLua;
using UnityEngine;
using UnityEngine.UI;

namespace CSTI_LuaActionSupport.AllPatcher;

[HarmonyPatch]
public static class ObjModifyPatcher
{
    [HarmonyPrefix, HarmonyPatch(typeof(GlobalCharacterInfo), nameof(GlobalCharacterInfo.LoadCustomPortraits)),
     HarmonyPatch(typeof(MainMenu), nameof(MainMenu.SelectPortrait), typeof(string)),
     HarmonyPatch(typeof(MainMenu), nameof(MainMenu.SelectPortrait), typeof(int)),
     HarmonyPatch(typeof(GlobalCharacterInfo), nameof(GlobalCharacterInfo.NextPortrait)),
     HarmonyPatch(typeof(GlobalCharacterInfo), nameof(GlobalCharacterInfo.PrevPortrait)),
     HarmonyPatch(typeof(GlobalCharacterInfo), nameof(GlobalCharacterInfo.GetCharacterPortrait)),
     HarmonyPatch(typeof(GlobalCharacterInfo), nameof(GlobalCharacterInfo.AlreadyHasCustomPortrait))
    ]
    public static void LoadPortrait()
    {
        PortraitInfo.LoadAllInQueue();
        var infos = Resources.FindObjectsOfTypeAll<GlobalCharacterInfo>();
        foreach (var characterInfo in infos)
        {
            var characterInfoPortraitSprites =
                characterInfo.PortraitSprites.ToDictionary(cpRef => cpRef.PortraitID, cpRef => cpRef);
            var all_characterInfoPortraitSprites =
                characterInfo.AllPortraits.ToDictionary(cpRef => cpRef.PortraitID, cpRef => cpRef);
            foreach (var (id, info) in PortraitInfo.PortraitInfos)
            {
                if (info.CachePortraitSprite == null) info.LoadSprite();
                if (info.CachePortraitSprite == null) return;
                characterInfoPortraitSprites[id] = new CharacterPortraitRef
                    {PortraitID = id, PortraitSprite = info.CachePortraitSprite};
                all_characterInfoPortraitSprites[id] = new CharacterPortraitRef
                    {PortraitID = id, PortraitSprite = info.CachePortraitSprite};
            }

            characterInfo.PortraitSprites = characterInfoPortraitSprites.Values.ToArray();
            characterInfo.AllPortraits = all_characterInfoPortraitSprites.Values.ToList();
        }
    }

    public static Sprite? IP_BG_BG;
    public static Sprite? IP_BG_FG;

    [HarmonyPatch(typeof(GraphicsManager), nameof(GraphicsManager.Init)), HarmonyPostfix]
    public static void GraphicsManager_Init(GraphicsManager __instance)
    {
        var ShadowAndPopupWithTitle = __instance.InventoryInspectionPopup.GetComponentsInChildren<RectTransform>()
            .First(transform => transform.name == "ShadowAndPopupWithTitle");
        var BookCover = ShadowAndPopupWithTitle.GetComponentsInChildren<Image>()
            .First(image => image.name == "BookCover");
        var Page = BookCover.GetComponentsInChildren<Image>().First(image => image.name == "Page");
        IP_BG_BG = BookCover.sprite;
        IP_BG_FG = Page.sprite;
    }

    public static void GraphicsManager_ReInit(this GraphicsManager __instance, Sprite? ip_bg_bg = null,
        Sprite? ip_bg_fg = null)
    {
        var ShadowAndPopupWithTitle = __instance.InventoryInspectionPopup
            .GetComponentsInChildren<RectTransform>()
            .First(transform => transform.name == "ShadowAndPopupWithTitle");
        var BookCover = ShadowAndPopupWithTitle.GetComponentsInChildren<Image>()
            .First(image => image.name == "BookCover");
        var Page = BookCover.GetComponentsInChildren<Image>().First(image => image.name == "Page");
        if (ip_bg_bg == null)
        {
            if (IP_BG_BG != null)
            {
                BookCover.sprite = IP_BG_BG;
            }
        }
        else
        {
            BookCover.sprite = ip_bg_bg;
        }

        if (ip_bg_fg == null)
        {
            if (IP_BG_FG != null)
            {
                Page.sprite = IP_BG_FG;
            }
        }
        else
        {
            Page.sprite = ip_bg_fg;
        }
    }

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

    public static string LuaFun2Desc(this LuaFunction function, string id)
    {
        var funcId = $"F_{(id + "desc").ToSha256().Substring(0, 6)}.desc__Lua__{id.ToSha256().Substring(0, 18)}_";
        GetModTable(MainBuilder.CurModId)[funcId] = function;
        return "###luaAction CardDescription\n" +
               $"Ret['ret']=GetModTable('{MainBuilder.CurModId}').{funcId}()" +
               "\n###";
    }
}