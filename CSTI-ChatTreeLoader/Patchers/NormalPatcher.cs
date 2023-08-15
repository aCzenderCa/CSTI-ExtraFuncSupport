using HarmonyLib;

namespace ChatTreeLoader.Patchers
{
    public static class NormalPatcher
    {
        public static Harmony HarmonyInstance;

        public static bool ShouldWaitExtra { get; set; }

        public static void DoPatch(Harmony harmony)
        {
            HarmonyInstance = harmony;
            if (HarmonyInstance is null) return;
            HarmonyInstance.PatchAll(typeof(ExtraStatImpl));
            HarmonyInstance.PatchAll(typeof(NormalPatcher));
            // HarmonyInstance.PatchAll(typeof(SlotChangePatcher));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.PerformingAction), MethodType.Getter)]
        public static void WaitExtraAct(ref bool __result)
        {
            __result |= ShouldWaitExtra;
        }
    }
}