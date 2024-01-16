using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using gfoidl.Base64;
using HarmonyLib;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

[HarmonyPatch]
public static class LuaSystem
{
    [HarmonyPostfix, HarmonyPatch(typeof(CardData), nameof(CardData.EnvironmentDictionaryKey))]
    private static void SuEnvironmentDictionaryKey(CardData __instance, CardData _FromEnvironment, int _ID,
        ref string __result)
    {
        if (!__instance.InstancedEnvironment) return;
        if (!_FromEnvironment.InstancedEnvironment) return;
        if (_FromEnvironment.UniqueID != GameManager.Instance.CurrentEnvironment.UniqueID) return;
        var newEnvKey = GetCurEnvId() ?? "";
        var sha256 = SHA256.Create();
        var memoryStream = new MemoryStream();
        var binaryWriter = new BinaryWriter(memoryStream);
        binaryWriter.Write(newEnvKey);
        binaryWriter.Write(newEnvKey + "_From");
        binaryWriter.Write(newEnvKey + "_Super");
        var encode = Base64.Default.Encode(sha256.ComputeHash(memoryStream.ToArray()));
        newEnvKey = encode + "_" + __instance.UniqueID;
        if (_ID != 0)
        {
            newEnvKey += "=" + GameManager.Instance.NextTravelIndex;
        }

        __result = newEnvKey;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardData), nameof(CardData.GenerateMoveToLocationAction))]
    private static void SuGenerateMoveToLocationAction(InGameCardBase _ReceivingCard,
        InGameCardBase _GivenCard, CardOnCardAction __result)
    {
        bool? isSendBack = null;
        if (!_ReceivingCard) return;
        if (!_GivenCard) return;
        if (_ReceivingCard.CardModel.CardInteractions is {Length: > 0})
        {
            foreach (var action in _ReceivingCard.CardModel.CardInteractions)
            {
                if (action == null) continue;
                if (!action.CardsAndTagsAreCorrect(_ReceivingCard, _GivenCard)) continue;
                if (action.TravelToPreviousEnv)
                {
                    isSendBack = true;
                    break;
                }

                var travelDestination = action.TravelDestination;
                if (!travelDestination) continue;
                isSendBack = false;
                break;
            }
        }

        if (isSendBack == null && _ReceivingCard.CardModel.DismantleActions is {Count: > 0})
        {
            foreach (var action in _ReceivingCard.CardModel.DismantleActions)
            {
                if (action == null) continue;
                if (action.TravelToPreviousEnv)
                {
                    isSendBack = true;
                    break;
                }

                var travelDestination = action.TravelDestination;
                if (!travelDestination) continue;
                isSendBack = false;
                break;
            }
        }

        if (isSendBack != true) return;
        __result.ExtraDurabilityModifications[0].SendToEnvironment = Array.Empty<EnvironmentCardDataRef>();
        __result.ActionName.LocalizationKey = "LuaCardOnCardAction_" + nameof(SuGenerateMoveToLocationAction);
        //lang=lua
        __result.ActionName.ParentObjectID = """
                                             LuaSystem.SendCard2BackEnvSave(receive, given)
                                             given:Remove(false, true)
                                             """;
    }

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

        public readonly List<(string, string)> Stack = new();

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
        __result = null;
        if (!GameManager.Instance.CardsLoaded) return true;
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

    [LuaFunc]
    public static void SendCard2EnvSave(string envUid, string envSaveId, CardAccessBridge? card)
    {
        if (!GameManager.Instance || UniqueIDScriptable.GetFromID<CardData>(envUid) is not { } envCard ||
            card == null || card.CardBase == null) return;
        var envSaveData = GameManager.Instance.SuperGetEnvSaveData(envCard, envSaveId)!;
        card.CardBase.Environment = envCard;
        if (card.CardBase.IndependentFromEnv)
        {
            card.CardBase.PrevEnvironment = GameManager.Instance.PrevEnvironment;
            card.CardBase.PrevEnvTravelIndex = 0;
        }
        else
        {
            card.CardBase.PrevEnvironment = null;
        }

        var slotInfo = new SlotInfo(GraphicsManager.Instance.CardToSlotType(card.CardBase.CardModel.CardType),
            envSaveData.AllRegularCards.Count + 1);
        card.CardBase.CreatedInSaveDataTick = GameManager.Instance.CurrentTickInfo.z;
        card.CardBase.IgnoreBaseRow = true;
        if (card.CardBase.IsInventoryCard || card.CardBase.IsLiquidContainer)
        {
            envSaveData.AllInventoryCards.Add(card.CardBase.SaveInventory(envSaveData.NestedInventoryCards, true));
            envSaveData.AllInventoryCards[envSaveData.AllInventoryCards.Count - 1].SlotInformation = slotInfo;
        }
        else
        {
            envSaveData.AllRegularCards.Add(card.CardBase.Save());
            envSaveData.AllRegularCards[envSaveData.AllRegularCards.Count - 1].SlotInformation = slotInfo;
        }

        if (envSaveData.CurrentMaxWeight > 0f)
        {
            envSaveData.CurrentWeight += card.CardBase.CurrentWeight;
        }
    }

    [LuaFunc]
    public static void SendCard2BackEnvSave(CardAccessBridge? by, CardAccessBridge? card)
    {
        var curReturnStack = CurReturnStack();
        if (curReturnStack.Peek() is var (uid, eid))
        {
            SendCard2EnvSave(uid, eid, card);
        }
        else
        {
            SendCard2EnvSave(GameManager.Instance.PrevEnvironment.UniqueID,
                GameManager.Instance.PrevEnvironment.EnvironmentDictionaryKey(GameManager.Instance.CurrentEnvironment,
                    by?.TravelIndex ?? 0), card);
        }
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
        AllSystems = new();

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

    public static readonly Dictionary<string, string> CnDict = new();

    public static string CnStr(this LocalizedString s)
    {
        if (CnDict.Count == 0)
        {
            var currentLanguage = LocalizationManager.CurrentLanguage;
            var languageSettings = LocalizationManager.Instance.Languages.ToList();
            var languageSetting =
                languageSettings.First(setting => setting.LanguageName == "简体中文");
            var index = languageSettings.IndexOf(languageSetting);
            LocalizationManager.Instance.ChangeLanguageOption = true;
            if (currentLanguage == index)
            {
                if (LocalizationManager.CurrentTexts == null || LocalizationManager.CurrentTexts.Count == 0)
                    LocalizationManager.LoadLanguage();
                foreach (var (key, text) in LocalizationManager.CurrentTexts!)
                {
                    CnDict[key] = text;
                }
            }
            else
            {
                LocalizationManager.SetLanguage(index, true);
                foreach (var (key, text) in LocalizationManager.CurrentTexts)
                {
                    CnDict[key] = text;
                }

                LocalizationManager.SetLanguage(currentLanguage, true);
            }
        }

        if (CnDict.TryGetValue(s.LocalizationKey, out var cn))
        {
            return cn;
        }

        return s;
    }
}