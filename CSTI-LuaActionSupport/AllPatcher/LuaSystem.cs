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