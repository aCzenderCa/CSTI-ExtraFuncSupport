using System;
using System.Collections;
using System.Collections.Generic;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

public class LuaRegister
{
    public static readonly LuaRegister Register = new();

    private LuaRegister()
    {
    }

    private readonly Dictionary<string, Dictionary<string, Dictionary<string, List<LuaFunction>>>> AllReg = new();

    public bool TryGet(string klass, string method, out Dictionary<string, List<LuaFunction>> regs)
    {
        if (AllReg.TryGetValue(klass, out var dictionary1) &&
            dictionary1.TryGetValue(method, out regs))
        {
            return true;
        }

        regs = null!;
        return false;
    }

    public bool TryGet(string klass, string method, string uid, out List<LuaFunction> regs)
    {
        if (AllReg.TryGetValue(klass, out var dictionary1) &&
            dictionary1.TryGetValue(method, out var dictionary2) &&
            dictionary2.TryGetValue(uid, out regs))
        {
            return true;
        }

        regs = null!;
        return false;
    }

    public void Reg(string klass, string method, string uid, LuaFunction function)
    {
        if (!AllReg.ContainsKey(klass))
        {
            AllReg[klass] = new Dictionary<string, Dictionary<string, List<LuaFunction>>>();
        }

        if (!AllReg[klass].ContainsKey(method))
        {
            AllReg[klass][method] = new Dictionary<string, List<LuaFunction>>();
        }

        if (!AllReg[klass][method].ContainsKey(uid))
        {
            AllReg[klass][method][uid] = new List<LuaFunction>();
        }

        AllReg[klass][method][uid].Add(function);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardGraphics), nameof(CardGraphics.CurrentImage), MethodType.Getter)]
    public static void Lua_CardGraphics_CurrentImageGetter(CardGraphics __instance, ref Sprite? __result)
    {
        if (!__instance.CardLogic || !__instance.CardLogic.CardModel) return;
        if (!Register.TryGet(nameof(CardGraphics), nameof(CardGraphics.CurrentImage) + "_Getter",
                __instance.CardLogic.CardModel.UniqueID, out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(__instance, new CardAccessBridge(__instance.CardLogic),
                    new SimpleUniqueAccess(__instance.CardLogic.CardModel), __result != null ? __result.name : null);
                if (objects.Length > 0 && objects[0] is string spriteName &&
                    SpriteDict.TryGetValue(spriteName, out var sprite))
                {
                    __result = sprite;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InspectionPopup), nameof(InspectionPopup.Setup), typeof(InGameCardBase))]
    public static void Lua_InspectionPopup_Setup(InspectionPopup __instance, InGameCardBase _Card)
    {
        GraphicsManager.Instance.GraphicsManager_ReInit();
        if (!_Card) return;
        if (!Register.TryGet(nameof(InspectionPopup), nameof(InspectionPopup.Setup) + "_ModBG",
                _Card.CardModel.UniqueID,
                out var regs_ModBG)) return;
        foreach (var luaFunction in regs_ModBG)
        {
            try
            {
                var objects = luaFunction.Call(__instance, new CardAccessBridge(_Card));
                if (objects.Length > 0 && objects[0] is string fg && SpriteDict.TryGetValue(fg, out var fg_sprite))
                {
                    if (objects.Length > 1 && objects[1] is string bg && SpriteDict.TryGetValue(bg, out var bg_sprite))
                    {
                        GraphicsManager.Instance.GraphicsManager_ReInit(bg_sprite, fg_sprite);
                    }
                    else
                    {
                        GraphicsManager.Instance.GraphicsManager_ReInit(null, fg_sprite);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardOnCardAction), nameof(CardOnCardAction.CardsAndTagsAreCorrect))]
    public static void Lua_CardOnCardAction_CardsAndTagsAreCorrect(CardOnCardAction __instance,
        InGameCardBase _Receiving, InGameCardBase _Given, ref bool __result)
    {
        if (!Register.TryGet(nameof(CardOnCardAction), nameof(CardOnCardAction.CardsAndTagsAreCorrect),
                __instance.ActionName.LocalizationKey, out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(__instance, new CardAccessBridge(_Receiving),
                    new CardAccessBridge(_Given), __result);
                if (objects.Length > 0 && objects[0] is bool flag)
                {
                    __result = flag;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardAction), nameof(CardAction.CardsAndTagsAreCorrect))]
    public static void Lua_CardAction_CardsAndTagsAreCorrect(CardAction __instance,
        InGameCardBase _ForCard, ref bool __result)
    {
        if (!Register.TryGet(nameof(CardAction), nameof(CardAction.CardsAndTagsAreCorrect),
                __instance.ActionName.LocalizationKey, out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(__instance, new CardAccessBridge(_ForCard), __result);
                if (objects.Length > 0 && objects[0] is bool flag)
                {
                    __result = flag;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameStat), nameof(InGameStat.CurrentValue))]
    public static void LuaGameStatValue(InGameStat __instance, bool _NotAtBase, ref float __result)
    {
        if (__instance == null || __instance.StatModel == null) return;
        if (!Register.TryGet(nameof(InGameStat), nameof(InGameStat.CurrentValue), __instance.StatModel.UniqueID,
                out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(new GameStatAccessBridge(__instance),
                    new SimpleUniqueAccess(__instance.StatModel), __result, _NotAtBase);
                if (objects.Length > 0 && objects[0] is { } val && val.TryNum<float>() is { } f)
                {
                    __result = f;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.CanReceiveInInventory))]
    public static void LuaCanReceiveInInventory(InGameCardBase __instance, CardData _Card, CardData _WithLiquid,
        ref bool __result)
    {
        if (__instance == null || __instance.CardModel == null) return;
        if (!Register.TryGet(nameof(InGameCardBase), nameof(InGameCardBase.CanReceiveInInventory),
                __instance.CardModel.UniqueID, out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(new CardAccessBridge(__instance), new SimpleUniqueAccess(_Card),
                    new SimpleUniqueAccess(_WithLiquid));
                if (objects.Length > 0 && objects[0] is bool flag)
                {
                    __result = flag;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.CanReceiveInInventoryInstance))]
    public static void LuaCanReceiveInInventoryInstance(InGameCardBase __instance, InGameCardBase _Card,
        ref bool __result)
    {
        if (__instance == null || __instance.CardModel == null) return;
        if (!Register.TryGet(nameof(InGameCardBase), nameof(InGameCardBase.CanReceiveInInventoryInstance),
                __instance.CardModel.UniqueID, out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(new CardAccessBridge(__instance), new CardAccessBridge(_Card));
                if (objects.Length > 0 && objects[0] is bool flag)
                {
                    __result = flag;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.InventoryWeight))]
    public static void LuaInventoryWeight(InGameCardBase __instance, ref float __result)
    {
        if (__instance == null || __instance.CardModel == null) return;
        if (!Register.TryGet(nameof(InGameCardBase), nameof(InGameCardBase.InventoryWeight),
                __instance.CardModel.UniqueID, out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(new CardAccessBridge(__instance), __result);
                if (objects.Length > 0)
                {
                    __result.TryModBy(objects[0]);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.CardName))]
    public static void LuaCardName(InGameCardBase __instance, bool _IgnoreLiquid, ref string __result)
    {
        if (__instance == null || __instance.CardModel == null) return;
        if (!Register.TryGet(nameof(InGameCardBase), nameof(InGameCardBase.CardName), __instance.CardModel.UniqueID,
                out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(new CardAccessBridge(__instance), _IgnoreLiquid);
                if (objects.Length > 0 && objects[0] is string s)
                {
                    __result = s;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.CardDescription))]
    public static void LuaCardDescription(InGameCardBase __instance, bool _IgnoreLiquid, ref string __result)
    {
        if (__instance == null || __instance.CardModel == null) return;
        if (!Register.TryGet(nameof(InGameCardBase), nameof(InGameCardBase.CardDescription),
                __instance.CardModel.UniqueID, out var regs)) return;
        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(new CardAccessBridge(__instance), _IgnoreLiquid);
                if (objects.Length > 0 && objects[0] is string s)
                {
                    __result = s;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ChangeStatValue))]
    public static void LuaChangeStat(GameManager __instance, InGameStat _Stat, float _Value,
        StatModification _Modification, ref IEnumerator __result)
    {
        if (_Stat == null || _Stat.StatModel == null) return;
        if (!Register.TryGet(nameof(GameManager), nameof(GameManager.ChangeStatValue),
                _Stat.StatModel.UniqueID, out _)) return;

        __result = __result.Concat(Inner(__instance));
        return;

        IEnumerator Inner(GameManager instance)
        {
            if (!Register.TryGet(nameof(GameManager), nameof(GameManager.ChangeStatValue),
                    _Stat.StatModel.UniqueID, out var regs)) yield break;
            foreach (var luaFunction in regs)
            {
                try
                {
                    luaFunction.Call(instance, new GameStatAccessBridge(_Stat), _Value, _Modification);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }

            var queue = instance.ProcessCache();
            while (queue.Count > 0)
            {
                var coroutineController = queue.Dequeue();
                while (coroutineController.state == CoroutineState.Running)
                {
                    yield return null;
                }
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ChangeStatRate))]
    public static void LuaChangeStatRate(GameManager __instance, InGameStat _Stat, float _Rate,
        StatModification _Modification, ref IEnumerator __result)
    {
        if (_Stat == null || _Stat.StatModel == null) return;
        if (!Register.TryGet(nameof(GameManager), nameof(GameManager.ChangeStatRate),
                _Stat.StatModel.UniqueID, out _)) return;

        __result = __result.Concat(Inner(__instance));
        return;

        IEnumerator Inner(GameManager instance)
        {
            if (!Register.TryGet(nameof(GameManager), nameof(GameManager.ChangeStatRate),
                    _Stat.StatModel.UniqueID, out var regs)) yield break;
            foreach (var luaFunction in regs)
            {
                try
                {
                    luaFunction.Call(instance, new GameStatAccessBridge(_Stat), _Rate, _Modification);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }

            var queue = instance.ProcessCache();
            while (queue.Count > 0)
            {
                var coroutineController = queue.Dequeue();
                while (coroutineController.state == CoroutineState.Running)
                {
                    yield return null;
                }
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(DismantleActionButton), nameof(DismantleActionButton.Setup))]
    public static void LuaDismantleActionButton_Setup(DismantleActionButton __instance, DismantleCardAction _Action,
        InGameCardBase _Card)
    {
        if (_Card == null || _Card.CardModel == null) return;
        if (!Register.TryGet(nameof(DismantleActionButton), nameof(DismantleActionButton.Setup),
                _Card.CardModel.UniqueID, out var regs)) return;

        foreach (var luaFunction in regs)
        {
            try
            {
                var objects = luaFunction.Call(__instance, _Action, new CardAccessBridge(_Card),
                    _Action.ActionName.LocalizationKey);
                if (objects.Length > 0 && objects[0] is bool show)
                {
                    __instance.gameObject.SetActive(show);
                }

                if (objects.Length > 1 && objects[1] is bool canUse)
                {
                    __instance.ConditionsValid = canUse;
                    __instance.Interactable = canUse;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }
}