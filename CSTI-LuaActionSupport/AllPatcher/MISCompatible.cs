using BepInEx;
using HarmonyLib;

namespace CSTI_LuaActionSupport.AllPatcher;

[BepInPlugin("zender.MakeItSimple.ModMain.CompatibleML", "MISCompatible", ModVersion)]
[BepInDependency("zender.MakeItSimple.ModMain", BepInDependency.DependencyFlags.SoftDependency)]
public class MISCompatible : BaseUnityPlugin
{
    static MISCompatible()
    {
        var methodInfo = AccessTools.Method(typeof(GameManager), nameof(GameManager.RemoveCard));
        var patchInfo = Harmony.GetPatchInfo(methodInfo);
        foreach (var prefix in patchInfo.Prefixes)
        {
            if (prefix.PatchMethod?.DeclaringType == null) continue;
            if (prefix.PatchMethod.DeclaringType.Name == "ReturnToPrevEnv" &&
                prefix.PatchMethod.Name == "HarmonyPrefixRemoveCard" &&
                prefix.PatchMethod.DeclaringType.Namespace == "MakeItSimple.ModPatchers.FixPatcher")
            {
                HarmonyInstance.Patch(prefix.PatchMethod, prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(MISCompatible), nameof(PassHarmonyPrefixRemoveCard_MIS))));
                // var ModMain = AccessTools.TypeByName("MakeItSimple.ModMain");
                // var ModMainHarmonyInstance = Traverse.Create(ModMain).Property<Harmony>("HarmonyInstance").Value;
                // ModMainHarmonyInstance.Unpatch(methodInfo, prefix.PatchMethod);
                break;
            }
        }
    }

    public static bool PassHarmonyPrefixRemoveCard_MIS(out bool __result)
    {
        __result = true;
        return false;
    }
}