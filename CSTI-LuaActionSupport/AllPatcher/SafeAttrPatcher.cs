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
    private static readonly Type HarmonyLib_PatchTools = AccessTools.TypeByName("HarmonyLib.PatchTools");

    private static readonly MethodInfo HarmonyLib_PatchTools_GetOriginalMethod =
        AccessTools.DeclaredMethod(HarmonyLib_PatchTools, "GetOriginalMethod");

    [HarmonyTargetMethod]
    public static MethodBase SafePatchWithAttributes_Target()
    {
        return AccessTools.Method(HarmonyLib_PatchTools, "GetPatchMethods");
    }

    [HarmonyPostfix]
    public static void SafePatchWithAttributes(IList __result)
    {
        List<int> needRemove = [];
        for (var index = 0; index < __result.Count; index++)
        {
            var o = __result[index];
            var traverse = Traverse.Create(o);
            var info = traverse.Field("info");
            var t = info.Field<Type>("declaringType").Value;
            var m = info.Field<string>("methodName").Value;
            var args = info.Field<Type[]>("argumentTypes").Value;
            var methodType = info.Field<MethodType>("methodType").Value;
            MethodBase? declaredMethod = null;
            try
            {
                switch (methodType)
                {
                    case MethodType.Normal:
                        declaredMethod = AccessTools.DeclaredMethod(t, m, args);
                        break;
                    case MethodType.Getter:
                        declaredMethod = AccessTools.DeclaredProperty(t, m)?.GetMethod;
                        break;
                    case MethodType.Setter:
                        declaredMethod = AccessTools.DeclaredProperty(t, m)?.SetMethod;
                        break;
                    case MethodType.Constructor:
                        declaredMethod = AccessTools.DeclaredConstructor(t, args);
                        break;
                    case MethodType.StaticConstructor:
                        declaredMethod = AccessTools.FirstConstructor(t, constructorInfo => constructorInfo.IsStatic);
                        break;
                    case MethodType.Enumerator:
                        declaredMethod = AccessTools.DeclaredMethod(t, m, args);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            if (declaredMethod == null)
            {
                needRemove.Add(index - needRemove.Count);
                Debug.Log($"[Warn]警告,应该存在的 {t.FullName}.{m} 不存在");
            }
        }

        foreach (var i in needRemove)
        {
            __result.RemoveAt(i);
        }
    }
}