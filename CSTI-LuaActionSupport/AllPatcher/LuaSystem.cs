using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using NLua;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSTI_LuaActionSupport.AllPatcher;

[HarmonyPatch]
public static class LuaSystem
{
    public static void SetupPatch(Harmony harmony)
    {
        harmony.PatchAll(typeof(LuaSystem));
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.CurrentEnvData))]
    public static bool CurrentEnvData(GameManager __instance, out EnvironmentSaveData? __result, bool _CreateIfNull)
    {
        __instance.EnvironmentsData.TryGetValue(GetCurEnvId() ?? "", out __result);
        if (__result == null && _CreateIfNull)
        {
            __result = __instance.SuperGetEnvSaveData(__instance.CurrentEnvironment, GetCurEnvId() ?? "");
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ChangeEnvironment))]
    public static bool GameManager_ChangeEnvironment(GameManager __instance, out IEnumerator __result)
    {
        __result = __instance.CommonChangeEnvironment();
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.InitializeDefaultCards))]
    public static void GameManager_OnInitializeDefaultCards()
    {
        var realEnv = GameManager.CurrentPlayerCharacter.Environment;
        foreach (var perk in GameManager.CurrentPlayerCharacter.CharacterPerks)
        {
            if (perk.OverrideEnvironment != null)
            {
                realEnv = perk.OverrideEnvironment;
            }

            if (perk.AddedCards?.FirstOrDefault(data => data.CardType == CardTypes.Environment) is { } envCard)
            {
                realEnv = envCard;
            }
        }

        SetCurEnvId(realEnv.EnvironmentDictionaryKey(realEnv, 0));
    }

    [LuaFunc]
    public static int GetCurTravelIndex()
    {
        if (!GameManager.Instance) return -10086;
        return GameManager.Instance.CurrentTravelIndex;
    }

    [LuaFunc]
    public static string? GetCurEnvId()
    {
        if (!GameManager.Instance) return null;
        return LoadCurrentSlot("CurEnvId") as string;
    }

    internal static void SetCurEnvId(string envId)
    {
        if (!GameManager.Instance) return;
        SaveCurrentSlot("CurEnvId", envId);
    }

    [LuaFunc]
    public static void AddCard2EnvSave(string envUid, string envSaveId, string cardId, int count)
    {
        if (!GameManager.Instance || UniqueIDScriptable.GetFromID<CardData>(envUid) is not { } envCard ||
            UniqueIDScriptable.GetFromID<CardData>(cardId) is not { } card) return;
        GameManager.Instance.SuperGetEnvSaveData(envCard, envSaveId);
        for (var i = 0; i < count; i++)
        {
            GameManager.Instance.CreateCardAsSaveData(card, envCard, envSaveId, null,
                null, true);
        }
    }

    // lang=lua
    [TestCode("""
              DebugBridge.debug = LuaSystem.GetCurEnvId()
              LuaSystem.SuperGoToEnv("67ff7f8557bae4c43a25f3bbfe56e063","__67ff7f8557bae4c43a25f3bbfe56e063")
              SimpleAccessTool["acefe4809985cf44db1fa3c9c3f518ac"]:Gen(2,{GenAfterEnvChange=true})
              """)]
    [LuaFunc]
    public static void SuperGoToEnv(string targetUid, string targetEnvId)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || UniqueIDScriptable.GetFromID<CardData>(targetUid) is not { } cd) return;
        gameManager.AddEnvCard(cd, null, targetEnvId).Add2AllEnumerators(PriorityEnumerators.EnvChange);
    }

    [LuaFunc]
    public static void GoToEnv(object cardData, int TravelIndex)
    {
        if (!GameManager.Instance) return;
        switch (cardData)
        {
            case string uid when UniqueIDScriptable.GetFromID<CardData>(uid) is { } cd:
                GameManager.Instance.NextEnvironment = cd;
                break;
            case SimpleUniqueAccess {UniqueIDScriptable: CardData cd}:
                GameManager.Instance.NextEnvironment = cd;
                break;
            default:
                return;
        }

        if (TravelIndex != -10086 && GameManager.Instance.NextEnvironment.InstancedEnvironment)
            GameManager.Instance.NextTravelIndex = TravelIndex;
        GameManager.Instance.AddCard(
                GameManager.Instance.NextEnvironment, null, true, null, true, SpawningLiquid.Empty,
                new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1), false)
            .Add2AllEnumerators(PriorityEnumerators.EnvChange);
    }

    public static readonly Dictionary<string, Dictionary<string, Dictionary<string, List<LuaFunction>>>>
        AllSystems = [];

    [LuaFunc]
    public static void AddSystem(string type, string sys_type, string uid, LuaFunction function)
    {
        AllSystems.SafeDAdd(type, sys_type, uid, function);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.LateUpdate))]
    public static void OnUpdate(InGameCardBase __instance)
    {
        if (__instance == null || __instance.CardModel == null || !__instance.Initialized) return;
        if (AllSystems.SafeDGet(nameof(InGameCardBase), nameof(OnUpdate), __instance.CardModel.UniqueID) is
            { } functions)
        {
            foreach (var luaFunction in functions)
            {
                try
                {
                    luaFunction.Call(new CardAccessBridge(__instance));
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.Init))]
    public static void PostInit(InGameCardBase __instance, ref IEnumerator __result)
    {
        __result = AddPost(__result);
        return;

        IEnumerator AddPost(IEnumerator rawEnum)
        {
            while (rawEnum.MoveNext())
            {
                yield return rawEnum.Current;
            }

            if (__instance == null || __instance.CardModel == null || !__instance.Initialized) yield break;
            if (AllSystems.SafeDGet(nameof(InGameCardBase), nameof(PostInit), __instance.CardModel.UniqueID) is
                { } functions)
            {
                foreach (var luaFunction in functions)
                {
                    try
                    {
                        luaFunction.Call(new CardAccessBridge(__instance));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }
                }
            }
        }
    }
}