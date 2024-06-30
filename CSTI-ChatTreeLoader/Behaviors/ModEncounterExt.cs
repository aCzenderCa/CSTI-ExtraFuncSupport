using System;
using System.Collections.Generic;
using ChatTreeLoader.ScriptObjects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ChatTreeLoader.Behaviors
{
    public static class ModEncounterExt
    {
        public static void RegAllEncounter<T>()
            where T : ModEncounterTypedBase<T>
        {
            if (!ModEncounterBase.AllModEncounters.ContainsKey(typeof(T)))
            {
                return;
            }

            foreach (var modEncounterBase in ModEncounterBase.AllModEncounters[typeof(T)])
            {
                modEncounterBase.Init();
            }
        }

        public static bool CheckId<T>(string id)
            where T : ModEncounterTypedBase<T>
        {
            if (!ModEncounterBase.AllModEncounters.ContainsKey(typeof(T)))
            {
                return false;
            }

            var modEncounterBases = ModEncounterBase.AllModEncounters[typeof(T)];
            if (modEncounterBases.Count == 0)
            {
                return false;
            }

            var modEncounterTypedBase = (ModEncounterTypedBase<T>) modEncounterBases[0];
            return modEncounterTypedBase.GetValidEncounterTable().ContainsKey(id);
        }

        public static bool DisplayModEncounterEx<T>(this EncounterPopup __instance)
            where T : ModEncounterTypedBase<T>
        {
            if (!__instance.CheckEnable<T>())
            {
                return true;
            }

            if (ModEncounterCodeImplBase.AllImpls.TryGetValue(typeof(T), out var codeImplBase))
            {
                codeImplBase.DisplayChatModEncounter(__instance);
            }

            return false;
        }

        public static bool DoModPlayerActionEx<T>(this EncounterPopup __instance, int _Action)
            where T : ModEncounterTypedBase<T>
        {
            if (!__instance.CheckEnable<T>())
            {
                return true;
            }

            if (ModEncounterCodeImplBase.AllImpls.TryGetValue(typeof(T), out var codeImplBase))
            {
                codeImplBase.DoModPlayerAction(__instance, _Action);
            }

            return false;
        }

        public static bool ModRoundStartEx<T>(this EncounterPopup __instance, bool _Loaded)
            where T : ModEncounterTypedBase<T>
        {
            if (!__instance.CheckEnable<T>())
            {
                return true;
            }

            if (ModEncounterCodeImplBase.AllImpls.TryGetValue(typeof(T), out var codeImplBase))
            {
                codeImplBase.ModRoundStart(__instance, _Loaded);
            }

            return false;
        }

        public static bool CheckEnable<T>(this EncounterPopup __instance)
            where T : ModEncounterTypedBase<T>
        {
            RegAllEncounter<T>();
            var currentEncounterModel = __instance.CurrentEncounter.EncounterModel;
            return CheckId<T>(currentEncounterModel.UniqueID);
        }

        public static void CheckButtonCountAndEnable(this EncounterPopup __instance, int count,
            Action<EncounterPopup, EncounterOptionButton, int> initAct)
        {
            while (__instance.ActionButtons.Count < count)
            {
                var encounterOptionButton =
                    Object.Instantiate(__instance.ActionButtonPrefab, __instance.ActionButtonsParent);
                __instance.ActionButtons.Add(encounterOptionButton);
                encounterOptionButton.OnClicked = (Action<int>) Delegate.Combine(encounterOptionButton.OnClicked,
                    new Action<int>(__instance.DoPlayerAction));
            }

            for (var i = 0; i < __instance.ActionButtons.Count; i++)
            {
                if (i >= count)
                {
                    __instance.ActionButtons[i].gameObject.SetActive(false);
                    continue;
                }

                initAct(__instance, __instance.ActionButtons[i], i);
            }
        }

        public static void UpdateModEx<T>(this EncounterPopup __instance)
        {
            if (ModEncounterCodeImplBase.AllImpls.TryGetValue(typeof(T), out var codeImplBase) &&
                codeImplBase.IsRunning)
            {
                codeImplBase.UpdateModEx(__instance);
            }
        }
    }

    public abstract class ModEncounterCodeImplBase
    {
        public static readonly Dictionary<Type, ModEncounterCodeImplBase> AllImpls = new()
        {
            {typeof(ModEncounter), new ChatEncounterExt()},
            {typeof(SimpleTraderEncounter), new TraderEncounterExt()}
        };

        public bool IsRunning;

        public abstract void DisplayChatModEncounter(EncounterPopup __instance);
        public abstract void DoModPlayerAction(EncounterPopup __instance, int _Action);
        public abstract void ModRoundStart(EncounterPopup __instance, bool _Loaded);
        public abstract void UpdateModEx(EncounterPopup __instance);
    }

    public abstract class ModEncounterExtBase<T> : ModEncounterCodeImplBase
        where T : ModEncounterTypedBase<T>
    {
    }

    public static class DeconstructHelper
    {
        public static void Deconstruct<TKey, TVal>(this KeyValuePair<TKey, TVal> pair, out TKey key, out TVal val)
        {
            key = pair.Key;
            val = pair.Value;
        }
    }
}