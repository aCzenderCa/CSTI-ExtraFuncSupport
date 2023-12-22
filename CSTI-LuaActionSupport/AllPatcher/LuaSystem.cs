using System;
using System.Collections;
using System.Collections.Generic;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

[HarmonyPatch]
public static class LuaSystem
{
    [LuaFunc]
    public static int GetCurTravelIndex()
    {
        if (!GameManager.Instance) return -10086;
        return GameManager.Instance.CurrentTravelIndex;
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
        Enumerators.Add(GameManager.Instance.AddCard(GameManager.Instance.NextEnvironment, null,
            true, null, true, SpawningLiquid.Empty, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1),
            false));
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