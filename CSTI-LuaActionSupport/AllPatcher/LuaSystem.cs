using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

[HarmonyPatch]
public static class LuaSystem
{
    // [HarmonyPostfix, HarmonyPatch(typeof(DynamicViewLayoutGroup), nameof(DynamicViewLayoutGroup.GetElementPosition))]
    // public static void GetElementPosition(DynamicViewLayoutGroup __instance, int _Index, ref Vector3 __result)
    // {
    //     if (__instance != GraphicsManager.Instance.BlueprintSlotsLine) return;
    //     float num = 0f;
    //     for (int i = 0; i < __instance.ExtraSpaces.Count; i++)
    //     {
    //         if (__instance.ExtraSpaces[i].AtIndex <= _Index)
    //         {
    //             num += __instance.ExtraSpaces[i].Space;
    //         }
    //     }
    //
    //     __result = __instance.LayoutOriginPos +
    //                __instance.LayoutDirection * (__instance.Spacing * (float) (_Index / 2) + num) +
    //                new Vector2(0, _Index % 2 == 1 ? 20 : 0);
    // }
    // public static readonly Dictionary<GameObject, int> ChildDictionary = new();
    //
    // [HarmonyPrefix, HarmonyPatch(typeof(HorizontalLayoutGroup), nameof(HorizontalLayoutGroup.SetLayoutVertical))]
    // public static void SetLayoutVertical(HorizontalLayoutGroup __instance)
    // {
    //     ChildDictionary.Clear();
    //     __instance.SetLayoutHorizontal();
    // }
    //
    // [HarmonyPrefix,
    //  HarmonyPatch(typeof(LayoutGroup), "SetChildAlongAxisWithScale", typeof(RectTransform)
    //      , typeof(int), typeof(float), typeof(float))]
    // public static void SetChildAlongAxisWithScale(LayoutGroup __instance, RectTransform rect,
    //     int axis, ref float pos, float scaleFactor)
    // {
    //     if (GraphicsManager.Instance == null) return;
    //     if (GraphicsManager.Instance.BlueprintModelsPopup == null) return;
    //     if (GraphicsManager.Instance.BlueprintModelsPopup.LockedBlueprintsParent == null) return;
    //     if (__instance != GraphicsManager.Instance.BlueprintModelsPopup.LockedBlueprintsParent.gameObject
    //             .GetComponent<HorizontalLayoutGroup>()) return;
    //     if (axis == 0)
    //     {
    //         var horizontalLayoutGroup = (HorizontalLayoutGroup) __instance;
    //         var size = rect.sizeDelta[axis] * scaleFactor + horizontalLayoutGroup.spacing;
    //         var _index = (int) (pos / size);
    //         ChildDictionary[rect.gameObject] = _index;
    //         pos = (_index / 2) * size;
    //     }
    //     else
    //     {
    //         var i = ChildDictionary.SafeGet(rect.gameObject) ?? 0;
    //         pos += i % 2 == 1 ? 20 : 0;
    //     }
    // }

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ProduceCards))]
    private static void SuProcessRetPre(ref bool _TravelToPrevEnv, out bool __state)
    {
        var curReturnStack = CurReturnStack();
        __state = _TravelToPrevEnv;
        if (curReturnStack.Peek() is null)
        {
            __state = false;
            return;
        }

        _TravelToPrevEnv = false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ProduceCards))]
    private static void SuProcessRetPost(bool __state, ref IEnumerator __result)
    {
        if (!__state) return;
        var curReturnStack = CurReturnStack();
        if (curReturnStack.Pop() is not var (uid, eid)) return;
        if (SuperGoToEnvAsEnum(uid, eid, false) is not { } retEnumerator) return;
        __result = __result.Prepend(retEnumerator);
    }

    [LuaFunc]
    public static ReturnStack CurReturnStack()
    {
        var o = LoadCurrentSlot(nameof(CurReturnStack));
        if (o is ReturnStack returnStack) return returnStack;

        var stack = new ReturnStack();
        SaveCurrentSlot(nameof(CurReturnStack), stack);
        return stack;
    }

    [LuaFunc]
    public static bool EnvReturn()
    {
        var curReturnStack = CurReturnStack();

        if (curReturnStack.Pop() is not var (uid, eid)) return false;
        SuperGoToEnv(uid, eid, false);
        return true;
    }

    public class ReturnStack
    {
        public static int MaxLength = 256;

        public readonly List<(string, string)> Stack = [];

        public void Push(string uid, string eid)
        {
            while (Stack.Count >= 256) Stack.RemoveAt(0);

            Stack.Add((uid, eid));
        }

        public (string uid, string eid)? Pop()
        {
            if (Stack.Count == 0) return null;
            var s_item = Stack[Stack.Count - 1];
            Stack.RemoveAt(Stack.Count - 1);
            return s_item;
        }

        public (string uid, string eid)? Peek()
        {
            if (Stack.Count == 0) return null;
            return Stack[Stack.Count - 1];
        }

        public void Save(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(Stack.Count);
            foreach (var (uid, eid) in Stack)
            {
                binaryWriter.Write(uid);
                binaryWriter.Write(eid);
            }
        }

        public void Load(BinaryReader binaryReader)
        {
            var count = binaryReader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var uid = binaryReader.ReadString();
                var eid = binaryReader.ReadString();
                Push(uid, eid);
            }
        }
    }

    public static void SetupPatch(Harmony harmony)
    {
        harmony.PatchAll(typeof(LuaSystem));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CurrentEnvData))]
    public static bool CurrentEnvData(GameManager __instance, out EnvironmentSaveData? __result, bool _CreateIfNull)
    {
        __instance.EnvironmentsData.TryGetValue(GetCurEnvId() ?? "", out __result);
        if (__result == null && _CreateIfNull)
            __result = __instance.SuperGetEnvSaveData(__instance.CurrentEnvironment, GetCurEnvId() ?? "");

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ChangeEnvironment))]
    public static bool GameManager_ChangeEnvironment(GameManager __instance, out IEnumerator __result)
    {
        __result = __instance.CommonChangeEnvironment();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.InitializeDefaultCards))]
    public static void GameManager_OnInitializeDefaultCards()
    {
        var realEnv = GameManager.CurrentPlayerCharacter.Environment;
        foreach (var perk in GameManager.CurrentPlayerCharacter.CharacterPerks)
        {
            if (perk.OverrideEnvironment != null) realEnv = perk.OverrideEnvironment;

            if (perk.AddedCards?.FirstOrDefault(data => data.CardType == CardTypes.Environment) is
                { } envCard) realEnv = envCard;
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
            GameManager.Instance.CreateCardAsSaveData(card, envCard, envSaveId, null,
                null, true);
    }

    // lang=lua
    [TestCode("""
              DebugBridge.debug = LuaSystem.GetCurEnvId()
              LuaSystem.SuperGoToEnv("67ff7f8557bae4c43a25f3bbfe56e063","__67ff7f8557bae4c43a25f3bbfe56e063")
              SimpleAccessTool["acefe4809985cf44db1fa3c9c3f518ac"]:Gen(2,{GenAfterEnvChange=true})
              """)]
    [LuaFunc]
    public static void SuperGoToEnv(string targetUid, string targetEnvId, bool modRetStack = true)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || UniqueIDScriptable.GetFromID<CardData>(targetUid) is not { } cd) return;
        gameManager.AddEnvCard(cd, null, targetEnvId, modRetStack).Add2AllEnumerators(PriorityEnumerators.EnvChange);
    }

    public static IEnumerator? SuperGoToEnvAsEnum(string targetUid, string targetEnvId, bool modRetStack = true)
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || UniqueIDScriptable.GetFromID<CardData>(targetUid) is not { } cd) return null;
        return gameManager.AddEnvCard(cd, null, targetEnvId, modRetStack);
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.LateUpdate))]
    public static void OnUpdate(InGameCardBase __instance)
    {
        if (__instance == null || __instance.CardModel == null || !__instance.Initialized) return;
        if (AllSystems.SafeDGet(nameof(InGameCardBase), nameof(OnUpdate), __instance.CardModel.UniqueID) is
            { } functions)
            foreach (var luaFunction in functions)
                try
                {
                    luaFunction.Call(new CardAccessBridge(__instance));
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.Init))]
    public static void PostInit(InGameCardBase __instance, ref IEnumerator __result)
    {
        __result = AddPost(__result);
        return;

        IEnumerator AddPost(IEnumerator rawEnum)
        {
            while (rawEnum.MoveNext()) yield return rawEnum.Current;

            if (__instance == null || __instance.CardModel == null || !__instance.Initialized) yield break;
            if (AllSystems.SafeDGet(nameof(InGameCardBase), nameof(PostInit), __instance.CardModel.UniqueID) is
                { } functions)
                foreach (var luaFunction in functions)
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