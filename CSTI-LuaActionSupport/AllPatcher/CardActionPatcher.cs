﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CSTI_LuaActionSupport.DataStruct;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaBuilder;
using CSTI_LuaActionSupport.LuaCodeHelper;
using CSTI_LuaActionSupport.UIStruct;
using gfoidl.Base64;
using HarmonyLib;
using NLua;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.LuaRegister;

namespace CSTI_LuaActionSupport.AllPatcher;

public static class CardActionPatcher
{
    public static readonly Lua LuaRuntime = new();
    public static readonly DebugBridge DebugBridge = new();
    public static readonly LuaTable ModData;
    public static readonly Dictionary<string, DataNode> GSaveData = new();
    public static readonly Dictionary<int, Dictionary<string, DataNode>> GSlotSaveData = new();
    private const string _InnerFuncBase = "CSTI_LuaActionSupport__InnerFunc";
    public static readonly SimpleAccessTool AccessTool = new();
    public static LuaTable InnerFuncBase => (LuaTable) LuaRuntime[_InnerFuncBase];

    public static LuaTable GetModTable(string modId)
    {
        if (ModData[modId] is LuaTable table)
        {
            return table;
        }

        var modDataName = nameof(ModData) + "." + modId;
        LuaRuntime.NewTable(modDataName);
        return LuaRuntime.GetTable(modDataName);
    }

    static CardActionPatcher()
    {
        LuaRuntime.State.OpenLibs();
        LuaRuntime.State.Encoding = Encoding.UTF8;
        LuaRuntime["std__debug"] = LuaRuntime["debug"];
        LuaRuntime["debug"] = DebugBridge;
        LuaRuntime[nameof(DebugBridge)] = DebugBridge;
        LuaRuntime[nameof(SimpleAccessTool)] = AccessTool;

        LuaRuntime.Register(typeof(DataAccessTool));
        LuaRuntime.Register(typeof(CardActionPatcher));
        LuaRuntime.Register(typeof(LuaTimer), nameof(LuaTimer));
        LuaRuntime.Register(typeof(LuaInput), nameof(LuaInput));
        LuaRuntime.Register(typeof(LuaGraphics), nameof(LuaGraphics));
        LuaRuntime.Register(typeof(LuaSystem), nameof(LuaSystem));
        LuaRuntime.Register(typeof(LuaAnim), nameof(LuaAnim));
        LuaRuntime.Register(typeof(MainBuilder), nameof(MainBuilder));
        LuaRuntime.Register(typeof(UIManagers), nameof(UIManagers));
        LuaRuntime.Register<CardTypes>();
        LuaRuntime.Register<DataNode.DataNodeType>();

        LuaRuntime[nameof(LuaEnum.Enum)] = LuaEnum.Enum;
        LuaRuntime[nameof(Register)] = Register;
        LuaRuntime.LoadCLRPackage();
        LuaRuntime.NewTable(nameof(ModData));
        ModData = LuaRuntime.GetTable(nameof(ModData));
        LuaRuntime[nameof(ModData)] = ModData;
    }

    public static Dictionary<string, DataNode> CurrentGSlotSaveData()
    {
        
        if (GSlotSaveData.TryGetValue(GameLoad.Instance.CurrentGameDataIndex, out var dataNodes))
        {
            return dataNodes;
        }

        GSlotSaveData[GameLoad.Instance.CurrentGameDataIndex] = new Dictionary<string, DataNode>();
        return GSlotSaveData[GameLoad.Instance.CurrentGameDataIndex];
    }

    // language=Lua
    [LuaFunc, TestCode("""
                       SaveCurrentSlot("__test",10)
                       """)]
    public static void SaveCurrentSlot(string key, object val)
    {
        CurrentGSlotSaveData().CommonSave(key, val);
    }

    public static LuaTable GetTempTable()
    {
        LuaRuntime.NewTable(nameof(ModData) + ".__TEMP__");
        return LuaRuntime.GetTable(nameof(ModData) + ".__TEMP__");
    }

    [LuaFunc]
    public static void SaveGlobal(string key, object val)
    {
        GSaveData.CommonSave(key, val);
    }

    [LuaFunc]
    public static object? LoadCurrentSlot(string key)
    {
        if (CurrentGSlotSaveData().TryGetValue(key, out var node))
        {
            return node.CommonLoad();
        }

        return "";
    }

    [LuaFunc]
    public static object? LoadGlobal(string key)
    {
        if (GSaveData.TryGetValue(key, out var node))
        {
            return node.CommonLoad();
        }

        return "";
    }

    private static object? CommonLoad(this in DataNode node)
    {
        switch (node.NodeType)
        {
            case DataNode.DataNodeType.Number:
                return node.number;
            case DataNode.DataNodeType.Str:
                return node.str;
            case DataNode.DataNodeType.Bool:
                return node._bool;
            case DataNode.DataNodeType.Table:
                return new DataNodeTableAccessBridge(node.table);
            case DataNode.DataNodeType.Nil:
                return null;
            case DataNode.DataNodeType.Vector2:
                return node.vector2;
            case DataNode.DataNodeType.IntTable:
                return new DataNodeTableAccessBridge(node.table);
            case DataNode.DataNodeType.ReturnStack:
                return node.ReturnStack;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static object? CommonLoad(this IDictionary<string, DataNode>? dataNodes, string key)
    {
        return dataNodes?.SafeGet(key)?.CommonLoad();
    }

    private static void CommonSave(this IDictionary<string, DataNode>? dataNodes, string key, object? val)
    {
        if (dataNodes == null) return;
        switch (val)
        {
            case null:
            {
                var dataNode = DataNode.Nil;
                dataNodes[key] = dataNode;
                break;
            }
            case double d:
            {
                var dataNode = new DataNode(d);
                dataNodes[key] = dataNode;
                break;
            }
            case long l:
            {
                var dataNode = new DataNode(l);
                dataNodes[key] = dataNode;
                break;
            }
            case int l:
            {
                var dataNode = new DataNode(l);
                dataNodes[key] = dataNode;
                break;
            }
            case string s:
            {
                var dataNode = new DataNode(s);
                dataNodes[key] = dataNode;
                break;
            }
            case bool b:
            {
                var dataNode = new DataNode(b);
                dataNodes[key] = dataNode;
                break;
            }
            case LuaTable table:
            {
                var nodes = new Dictionary<string, DataNode>();
                foreach (var tableKey in table.Keys)
                {
                    if (tableKey is string strKey &&
                        table[strKey] is (double or long or string or bool or null or LuaTable) and var tableVal)
                    {
                        nodes.CommonSave(strKey, tableVal);
                    }
                }

                var dataNode = new DataNode(nodes);
                dataNodes[key] = dataNode;
                break;
            }
            case Dictionary<string, object> dictionary:
            {
                var nodes = new Dictionary<string, DataNode>();
                foreach (var tableKey in dictionary.Keys)
                {
                    if (dictionary[tableKey] is (double or long or string or bool or null or LuaTable
                        or Dictionary<string, object>) and var tableVal)
                    {
                        nodes.CommonSave(tableKey, tableVal);
                    }
                }

                var dataNode = new DataNode(nodes);
                dataNodes[key] = dataNode;
                break;
            }
            case LuaSystem.ReturnStack returnStack:
            {
                dataNodes[key] = new DataNode(returnStack);
                break;
            }
        }
    }

    public class DataNodeTableAccessBridge
    {
        public CollectionDropsSaveData IntoSave()
        {
            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);
            new DataNode(Table ?? new Dictionary<string, DataNode>()).Save(binaryWriter);
            var array = memoryStream.ToArray();
            return new CollectionDropsSaveData($"LNbt|>{Base64.Default.Encode(array)}<|", Vector2Int.zero);
        }

        public LuaTable? LuaTable
        {
            get
            {
                if (Table == null) return null;
                var tempTable = LuaRuntime.TempTable();
                foreach (var (key, value) in Table)
                {
                    if (value.NodeType == DataNode.DataNodeType.Nil) continue;
                    var commonLoad = value.CommonLoad();

                    if (commonLoad is DataNodeTableAccessBridge bridge) tempTable[key] = bridge.LuaTable;
                    else tempTable[key] = commonLoad;
                }

                return tempTable;
            }
            set
            {
                if (value == null) return;
                foreach (var key in value.Keys)
                {
                    if (key is not string s_key) continue;
                    Table.CommonSave(s_key, value[s_key]);
                }
            }
        }

        public LuaTable? LuaKeys
        {
            get
            {
                if (Keys == null) return null;
                var luaTable = LuaRuntime.TempTable();
                foreach (var key in Keys)
                {
                    luaTable[luaTable.Keys.Count + 1] = key;
                }

                return luaTable;
            }
        }

        public object? this[string key]
        {
            get
            {
                if (Table?.TryGetValue(key, out var node) ?? false)
                {
                    return node.CommonLoad();
                }

                return null;
            }
            set => CommonSave(Table, key, value);
        }

        public readonly Dictionary<string, DataNode>? Table;

        public DataNodeTableAccessBridge(Dictionary<string, DataNode>? table)
        {
            Table = table;
        }

        public Dictionary<string, DataNode>.KeyCollection? Keys => Table?.Keys;
        public int Count => Table?.Count ?? 0;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardAction), nameof(CardAction.WillHaveAnEffect))]
    private static void LuaActionWillHaveAnEffect(CardAction __instance, ref bool __result)
    {
        if (__instance.ActionName.LocalizationKey?.StartsWith("LuaCardAction") is true ||
            __instance.ActionName.LocalizationKey?.StartsWith("LuaCardOnCardAction") is true ||
            __instance.ActionName.LocalizationKey?.StartsWith("LuaDismantleCardAction") is true ||
            __instance.ActionName.LocalizationKey?.StartsWith("CardActionPack") is true ||
            __instance.ActionName.LocalizationKey?.StartsWith("CardOnCardActionPack") is true)
        {
            __result = true;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardAction), nameof(CardAction.CardsAndTagsAreCorrect))]
    public static void LuaCardActionQuickRequirementsCheck(CardAction __instance, InGameCardBase _ForCard,
        ref bool __result)
    {
        if (__instance.ActionName.LocalizationKey?.StartsWith("CardActionPack") is true)
        {
            if (CardActionPack.GetActionPack(__instance.ActionName.ParentObjectID) is { } pack)
            {
                InGameCardBase? give = null;
                __result &= pack.CheckCondition(GameManager.Instance, _ForCard, ref give);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardOnCardAction), nameof(CardOnCardAction.CardsAndTagsAreCorrect),
         typeof(InGameCardBase), typeof(InGameCardBase), typeof(List<CardData>), typeof(List<CardTag>))]
    public static void LuaCardOnCardActionCardsAndTagsAreCorrect(CardAction __instance, InGameCardBase _Receiving,
        InGameCardBase _Given, ref bool __result)
    {
        if (__instance.ActionName.LocalizationKey?.StartsWith("CardActionPack") is true ||
            __instance.ActionName.LocalizationKey?.StartsWith("CardOnCardActionPack") is true)
        {
            if (CardActionPack.GetActionPack(__instance.ActionName.ParentObjectID) is { } pack)
            {
                InGameCardBase? give = _Given;
                __result &= pack.CheckCondition(GameManager.Instance, _Receiving, ref give);
            }
        }
    }

    [HarmonyPostfix,
     HarmonyPatch(typeof(DismantleActionButton), nameof(DismantleActionButton.Setup), typeof(int),
         typeof(DismantleCardAction), typeof(InGameCardBase), typeof(bool), typeof(bool))]
    private static void LuaDismantleActionButton_Setup(DismantleActionButton __instance, int _Index,
        DismantleCardAction _Action, InGameCardBase _Card)
    {
        if (_Action.ActionName.LocalizationKey?.StartsWith("LuaDismantleCardAction") is true)
        {
            var luaScriptRetValues = new LuaScriptRetValues();
            var lua = InitRuntime(GameManager.Instance);
            lua["Ret"] = luaScriptRetValues;
            lua.DoString(_Action.ActionName.ParentObjectID);
            if (luaScriptRetValues["OnSetup"] is LuaFunction OnSetup)
            {
                var objects = OnSetup.Call(__instance, _Action, new CardAccessBridge(_Card),
                    _Action.ActionName.LocalizationKey);
                if (objects.Length > 0 && objects[0] is bool show)
                {
                    __instance.gameObject.SafeSetActive(show);
                }

                if (objects.Length > 1 && objects[1] is bool interact)
                {
                    __instance.ConditionsValid = interact;
                    __instance.Interactable = interact;
                }
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ActionRoutine))]
    private static void LuaCardAction(CardAction _Action, InGameCardBase _ReceivingCard, GameManager __instance,
        ref IEnumerator __result)
    {
        if (_Action.ActionName.LocalizationKey?.StartsWith("LuaCardAction") is true)
        {
            __result = __result.Prepend(LuaCardActionHelper(_Action, _ReceivingCard, __instance));
        }

        if (_Action is DismantleCardAction action &&
            _Action.ActionName.LocalizationKey?.StartsWith("LuaDismantleCardAction") is true)
        {
            __result = __result.Prepend(LuaDismantleCardActionHelper(action, _ReceivingCard, __instance));
        }

        if (_Action.ActionName.LocalizationKey?.StartsWith("CardActionPack") is true &&
            CardActionPack.GetActionPack(_Action.ActionName.ParentObjectID) is { } pack)
        {
            __result = __result.Prepend(pack.ProcessAction(__instance, _ReceivingCard, null,
                _Action));
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.CardOnCardActionRoutine))]
    private static void LuaCardOnCardAction(CardOnCardAction _Action, InGameCardBase _ReceivingCard,
        InGameCardBase _GivenCard, GameManager __instance, ref IEnumerator __result)
    {
        if (_Action.ActionName.LocalizationKey?.StartsWith("LuaCardOnCardAction") is true)
        {
            __result = __result.Prepend(LuaCardOnCardActionHelper(_Action, _ReceivingCard, _GivenCard, __instance));
        }

        if (_Action.ActionName.LocalizationKey?.StartsWith("CardOnCardActionPack") is true &&
            CardActionPack.GetActionPack(_Action.ActionName.ParentObjectID) is { } pack)
        {
            __result = __result.Prepend(pack.ProcessAction(__instance, _ReceivingCard, _GivenCard,
                _Action));
        }
    }

    public class PriorityEnumerators
    {
        public const int BeforeAll = 1000;
        public const int BeforeTimeChange = 300;
        public const int SuperHigh = 200;
        public const int High = 100;
        public const int Normal = 0;
        public const int Low = -100;
        public const int SuperLow = -200;
        public const int EnvChange = -1000;
        public const int AfterEnvChange = -2000;
        public readonly List<List<IEnumerator>> ThisEnumerators;
        public readonly int Priority;

        public static PriorityEnumerators Get(int priority)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (AllEnumerators == null) return new PriorityEnumerators(new List<List<IEnumerator>>(), priority);
            if (AllEnumerators.TryGetValue(priority, out var priorityEnumerators))
                return priorityEnumerators;
            AllEnumerators[priority] = new PriorityEnumerators(new List<List<IEnumerator>>(), priority);
            return AllEnumerators[priority];
        }

        public PriorityEnumerators(List<List<IEnumerator>> _Enumerators, int _Priority)
        {
            ThisEnumerators = _Enumerators;
            Priority = _Priority;
        }
    }

    public static void Add2Li(this IEnumerator item, List<IEnumerator> list)
    {
        list.Add(item);
    }

    public static void Add2AllEnumerators(this IEnumerator? enumerator, int _Priority)
    {
        if (enumerator == null) return;
        PriorityEnumerators.Get(_Priority).ThisEnumerators.Add(new List<IEnumerator> {enumerator});
    }

    public static void Add2AllEnumerators(this List<IEnumerator> enumerators, int _Priority)
    {
        PriorityEnumerators.Get(_Priority).ThisEnumerators.Add(enumerators);
    }

    public static readonly Dictionary<int, PriorityEnumerators> AllEnumerators = new()
    {
        {PriorityEnumerators.BeforeAll, PriorityEnumerators.Get(PriorityEnumerators.BeforeAll)},
        {PriorityEnumerators.BeforeTimeChange, PriorityEnumerators.Get(PriorityEnumerators.BeforeTimeChange)},
        {PriorityEnumerators.SuperHigh, PriorityEnumerators.Get(PriorityEnumerators.SuperHigh)},
        {PriorityEnumerators.High, PriorityEnumerators.Get(PriorityEnumerators.High)},
        {PriorityEnumerators.Normal, PriorityEnumerators.Get(PriorityEnumerators.Normal)},
        {PriorityEnumerators.Low, PriorityEnumerators.Get(PriorityEnumerators.Low)},
        {PriorityEnumerators.SuperLow, PriorityEnumerators.Get(PriorityEnumerators.SuperLow)},
        {PriorityEnumerators.EnvChange, PriorityEnumerators.Get(PriorityEnumerators.EnvChange)},
        {PriorityEnumerators.AfterEnvChange, PriorityEnumerators.Get(PriorityEnumerators.AfterEnvChange)},
    };

    public static Lua InitRuntime(GameManager __instance)
    {
        LuaRuntime["gameManager"] = __instance;
        LuaRuntime["env"] = new CardAccessBridge(__instance.CurrentEnvironmentCard);
        LuaRuntime["exp"] = new CardAccessBridge(__instance.CurrentExplorableCard);
        LuaRuntime["weather"] = new CardAccessBridge(__instance.CurrentWeatherCard);
        return LuaRuntime;
    }

    public static IEnumerator LuaCardActionHelper(CardAction _Action, InGameCardBase _ReceivingCard,
        GameManager __instance)
    {
        var lua = InitRuntime(__instance);
        var waitTime = 0;
        var miniWaitTime = 0;
        var tickWaitTime = 0;
        lua["receive"] = new CardAccessBridge(_ReceivingCard);
        var luaScriptRetValues = new LuaScriptRetValues();
        lua["Ret"] = luaScriptRetValues;
        try
        {
            object? result;
            object? miniTime;
            object? tickTime;
            _ = lua.DoString(_Action.ActionName.ParentObjectID, _Action.ActionName.LocalizationKey);
            if (luaScriptRetValues.CheckKey(nameof(result), out result))
            {
                waitTime.TryModBy(result);
            }

            if (luaScriptRetValues.CheckKey(nameof(miniTime), out miniTime))
            {
                miniWaitTime.TryModBy(miniTime);
            }

            if (luaScriptRetValues.CheckKey(nameof(tickTime), out tickTime))
            {
                tickWaitTime.TryModBy(tickTime);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        var queue = __instance.ProcessTime(_ReceivingCard, waitTime, miniWaitTime, tickWaitTime).ProcessCache();

        while (queue.Count > 0)
        {
            var coroutineController = queue.Dequeue();
            if (coroutineController == null) break;
            while (coroutineController.state == CoroutineState.Running)
            {
                yield return null;
            }
        }
    }

    public static IEnumerator LuaDismantleCardActionHelper(DismantleCardAction _Action, InGameCardBase _ReceivingCard,
        GameManager __instance)
    {
        var luaScriptRetValues = new LuaScriptRetValues();
        var lua = InitRuntime(GameManager.Instance);
        lua["Ret"] = luaScriptRetValues;
        var waitTime = 0;
        var miniWaitTime = 0;
        var tickWaitTime = 0;
        try
        {
            object? result;
            object? miniTime;
            object? tickTime;
            lua.DoString(_Action.ActionName.ParentObjectID);
            if (luaScriptRetValues["OnAct"] is LuaFunction OnAct)
            {
                lua["receive"] = new CardAccessBridge(_ReceivingCard);
                OnAct.Call();
            }

            if (luaScriptRetValues.CheckKey(nameof(result), out result))
            {
                waitTime.TryModBy(result);
            }

            if (luaScriptRetValues.CheckKey(nameof(miniTime), out miniTime))
            {
                miniWaitTime.TryModBy(miniTime);
            }

            if (luaScriptRetValues.CheckKey(nameof(tickTime), out tickTime))
            {
                tickWaitTime.TryModBy(tickTime);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        var queue = __instance.ProcessTime(_ReceivingCard, waitTime, miniWaitTime, tickWaitTime).ProcessCache();
        while (queue.Count > 0)
        {
            var coroutineController = queue.Dequeue();
            if (coroutineController == null) break;
            while (coroutineController.state == CoroutineState.Running)
            {
                yield return null;
            }
        }
    }

    public static IEnumerator LuaCardOnCardActionHelper(CardAction _Action, InGameCardBase _ReceivingCard,
        InGameCardBase _GivenCard, GameManager __instance)
    {
        var lua = InitRuntime(__instance);
        var waitTime = 0;
        var miniWaitTime = 0;
        var tickWaitTime = 0;
        lua["receive"] = new CardAccessBridge(_ReceivingCard);
        lua["given"] = new CardAccessBridge(_GivenCard);
        var luaScriptRetValues = new LuaScriptRetValues();
        lua["Ret"] = luaScriptRetValues;
        try
        {
            object? result;
            object? miniTime;
            object? tickTime;
            _ = lua.DoString(_Action.ActionName.ParentObjectID, _Action.ActionName.LocalizationKey);
            if (luaScriptRetValues.CheckKey(nameof(result), out result))
            {
                waitTime.TryModBy(result);
            }

            if (luaScriptRetValues.CheckKey(nameof(miniTime), out miniTime))
            {
                miniWaitTime.TryModBy(miniTime);
            }

            if (luaScriptRetValues.CheckKey(nameof(tickTime), out tickTime))
            {
                tickWaitTime.TryModBy(tickTime);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        var queue = __instance.ProcessTime(_ReceivingCard, waitTime, miniWaitTime, tickWaitTime).ProcessCache();

        while (queue.Count > 0)
        {
            var coroutineController = queue.Dequeue();
            if (coroutineController == null) break;
            while (coroutineController.state == CoroutineState.Running)
            {
                yield return null;
            }
        }
    }

    public static GameManager ProcessTime(this GameManager manager,
        InGameCardBase _ReceivingCard,
        int waitTime, int miniWaitTime, int tickWaitTime)
    {
        var __instance = GameManager.Instance;

        if (tickWaitTime > 0)
        {
            if (LoadCurrentSlot($"C#Used__{nameof(tickWaitTime)}") is { } o)
            {
                tickWaitTime += o.TryNum<int>() ?? 0;
            }

            miniWaitTime += tickWaitTime / 10;
            tickWaitTime %= 10;
            SaveCurrentSlot($"C#Used__{nameof(tickWaitTime)}", tickWaitTime);
        }

        if (miniWaitTime > 0)
        {
            GameManager.Instance.CurrentMiniTicks += miniWaitTime;
            waitTime += GameManager.Instance.CurrentMiniTicks / 5;
            GameManager.Instance.CurrentMiniTicks %= 5;
        }

        if (waitTime > 0)
        {
            __instance.SpendDaytimePoints(waitTime, _ReceivingCard).Add2AllEnumerators(PriorityEnumerators.SuperHigh);
        }
        else
        {
            Runtime.Trans2Enum(GraphicsManager.Instance.UpdateTimeInfo, false)
                .Add2AllEnumerators(PriorityEnumerators.SuperHigh);
        }

        return manager;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.Init))]
    public static void OnCardInit(InGameCardBase __instance)
    {
        if (__instance.CardModel == null) return;
        if (__instance.IsPinned) return;
        var acted = false;
        foreach (var dismantleAction in __instance.CardModel.DismantleActions)
        {
            var cardAccessBridge = new CardAccessBridge(__instance);
            cardAccessBridge.InitData();
            if (dismantleAction.ActionName.LocalizationKey?.StartsWith("CardActionPack") is true &&
                CardActionPack.GetActionPack(dismantleAction.ActionName.ParentObjectID) is
                    {actOnCardInit: true} pack &&
                cardAccessBridge.Data![pack.uid] is not true)
            {
                acted = true;
                cardAccessBridge.Data[pack.uid] = true;
                cardAccessBridge.SaveData();
                var luaScriptRetValues = new LuaScriptRetValues();
                pack.Act(GameManager.Instance, __instance, null, luaScriptRetValues, dismantleAction);
            }
        }

        var accessBridge = new CardAccessBridge(__instance);
        if (accessBridge.Data is { } data && data[nameof(CommonCardGen.ActOnCardGen)] is string cardActionPackId
                                          && CardActionPack.GetActionPack(cardActionPackId) is { } cardActionPack)
        {
            var luaScriptRetValues = new LuaScriptRetValues();
            cardActionPack.Act(GameManager.Instance, __instance, null, luaScriptRetValues, null);
            var tp = luaScriptRetValues["result"].TryNum<int>() ?? 0;
            var miniTp = luaScriptRetValues["miniTime"].TryNum<int>() ?? 0;
            var tickTp = luaScriptRetValues["tickTime"].TryNum<int>() ?? 0;
            GameManager.Instance.ProcessTime(__instance, tp, miniTp, tickTp);
            acted = true;
            accessBridge = new CardAccessBridge(__instance);
            accessBridge.InitData();
            accessBridge.Data![nameof(CommonCardGen.ActOnCardGen)] = null;
            accessBridge.SaveData();
        }

        if (!acted)
        {
            return;
        }

        LuaTimer.Wait4CA();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.ApplyPassiveEffect))]
    public static void OnApplyPassiveEffect(InGameCardBase __instance, ref IEnumerator __result, PassiveEffect _Effect)
    {
        if (SimpleVarModEntry.SubStrC(_Effect.EffectName, "CAP|", out var id) &&
            CardActionPack.GetActionPack(id) is { } cap)
        {
            __result = __result.Concat(cap.ProcessAction(GameManager.Instance, __instance, null, null));
        }
    }
}