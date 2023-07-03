using System;
using ChatTreeLoader.Behaviors;
using ChatTreeLoader.ScriptObjects;
using HarmonyLib;
using UnityEngine;

namespace ChatTreeLoader.Patchers
{
    public static class MainPatcher
    {
        public static bool DoPatch(Harmony harmony)
        {
            try
            {
                harmony.PatchAll(typeof(MainPatcher));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            return true;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.DisplayPlayerActions))]
        public static bool DisplayModEncounter(EncounterPopup __instance)
        {
            return __instance.DisplayModEncounterEx<ModEncounter>() &&
                   __instance.DisplayModEncounterEx<SimpleTraderEncounter>();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.DoPlayerAction))]
        public static bool DoModPlayerAction(EncounterPopup __instance, int _Action)
        {
            return __instance.DoModPlayerActionEx<ModEncounter>(_Action) &&
                   __instance.DoModPlayerActionEx<SimpleTraderEncounter>(_Action);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.RoundStart))]
        public static bool ModRoundStart(bool _Loaded, EncounterPopup __instance)
        {
            return __instance.ModRoundStartEx<ModEncounter>(_Loaded) &&
                   __instance.ModRoundStartEx<SimpleTraderEncounter>(_Loaded);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.Update))]
        public static void UpdateMod(EncounterPopup __instance)
        {
            __instance.UpdateModEx<ModEncounter>();
            __instance.UpdateModEx<SimpleTraderEncounter>();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.CalculateStealthChecks))]
        public static bool CalculateModStealthChecks(EncounterPopup __instance)
        {
            return !(__instance.CheckEnable<ModEncounter>() || __instance.CheckEnable<SimpleTraderEncounter>());
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.ApplyEncounterResult))]
        public static bool ApplyEncounterResultMod(EncounterPopup __instance, out bool __result)
        {
            __result = false;
            if (!__instance.CheckEnable<ModEncounter>() && !__instance.CheckEnable<SimpleTraderEncounter>())
                return true;

            if (__instance.CurrentEncounter.EncounterResult == EncounterResult.Ongoing)
            {
                return false;
            }

            __result = true;
            return false;
        }
    }
}