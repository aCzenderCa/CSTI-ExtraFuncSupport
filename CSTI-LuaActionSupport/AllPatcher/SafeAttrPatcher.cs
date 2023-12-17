using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

[HarmonyPatch]
public static class SafeAttrPatcher
{
    [HarmonyTargetMethod]
    public static MethodBase SafePatchWithAttributes_Target()
    {
        var type = typeof(Harmony).Assembly.GetType("HarmonyLib.PatchTools");
        return AccessTools.Method(type, "GetPatchMethods");
    }

    [HarmonyPostfix]
    public static void SafePatchWithAttributes(IList __result)
    {
        List<int> needRemove = [];
        for (var index = 0; index < __result.Count; index++)
        {
            var o = __result[index];
            var traverse = Traverse.Create(o);
            if (traverse.Field("info") is var info && info.Method("GetOriginalMethod").GetValue<MethodBase>() is null)
            {
                needRemove.Add(index - needRemove.Count);
                Debug.LogError($"[Warn]重要警告,应该存在的 {info.Field("assemblyQualifiedDeclaringTypeName").GetValue()}.{info.Field("methodName").GetValue()} 不存在");
            }
        }

        foreach (var i in needRemove)
        {
            __result.RemoveAt(i);
        }
    }
}