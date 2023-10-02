﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher
{
    public static class CardActionPatcher
    {
        public static readonly Lua LuaRuntime = new();
        public static readonly DebugBridge DebugBridge = new();
        public static readonly LuaTable ModData;
        public static readonly Dictionary<string, DataNode> GSaveData = new();
        public static readonly Dictionary<int, Dictionary<string, DataNode>> GSlotSaveData = new();


        static CardActionPatcher()
        {
            LuaRuntime.State.OpenLibs();
            LuaRuntime.State.Encoding = Encoding.UTF8;
            LuaRuntime["debug"] = DebugBridge;
            LuaRuntime[nameof(SimpleAccessTool)] = new SimpleAccessTool();
            LuaRuntime["GetCard"] = DataAccessTool.GetCardDataIns;
            LuaRuntime["GetGameCard"] = DataAccessTool.GetCardAccessBridgeIns;
            LuaRuntime["GetGameCards"] = DataAccessTool.GetCardAccessBridgesIns;
            LuaRuntime["GetStat"] = DataAccessTool.GetStatModelIns;
            LuaRuntime["GetGameStat"] = DataAccessTool.GetGameStatAccessBridgeIns;
            LuaRuntime["CountCardOnBoard"] = DataAccessTool.CountCardOnBoardIns;
            LuaRuntime["CountCardInBase"] = DataAccessTool.CountCardInBaseIns;
            LuaRuntime["CountCardInHand"] = DataAccessTool.CountCardInHandIns;
            LuaRuntime["CountCardInLocation"] = DataAccessTool.CountCardInLocationIns;
            LuaRuntime[nameof(SaveCurrentSlot)] = (Action<string, object>) SaveCurrentSlot;
            LuaRuntime[nameof(SaveGlobal)] = (Action<string, object>) SaveGlobal;
            LuaRuntime[nameof(LoadCurrentSlot)] = (Func<string, object?>) LoadCurrentSlot;
            LuaRuntime[nameof(LoadGlobal)] = (Func<string, object?>) LoadGlobal;
            LuaRuntime.NewTable("DontAccessByRawPath__UseByCSharp___________ModData");
            LuaRuntime.LoadCLRPackage();
            ModData = LuaRuntime.GetTable("DontAccessByRawPath__UseByCSharp___________ModData");
            LuaRuntime[nameof(ModData)] = ModData;
        }

        private static Dictionary<string, DataNode> CurrentGSlotSaveData()
        {
            if (GSlotSaveData.TryGetValue(GameLoad.Instance.CurrentGameDataIndex, out var dataNodes))
            {
                return dataNodes;
            }

            GSlotSaveData[GameLoad.Instance.CurrentGameDataIndex] = new Dictionary<string, DataNode>();
            return GSlotSaveData[GameLoad.Instance.CurrentGameDataIndex];
        }

        public static void SaveCurrentSlot(string key, object val)
        {
            CurrentGSlotSaveData().CommonSave(key, val);
        }

        public static void SaveGlobal(string key, object val)
        {
            GSaveData.CommonSave(key, val);
        }

        public static object? LoadCurrentSlot(string key)
        {
            if (CurrentGSlotSaveData().TryGetValue(key, out var node))
            {
                return node.CommonLoad();
            }

            return "";
        }

        public static object? LoadGlobal(string key)
        {
            if (GSaveData.TryGetValue(key, out var node))
            {
                return node.CommonLoad();
            }

            return "";
        }

        private static object? CommonLoad(this ref DataNode node)
        {
            return node.NodeType switch
            {
                DataNode.DataNodeType.Number => node.number,
                DataNode.DataNodeType.Str => node.str,
                DataNode.DataNodeType.Bool => node._bool,
                DataNode.DataNodeType.Table => new DataNodeTableAccessBridge(node.table),
                DataNode.DataNodeType.Nil => null,
                _ => throw new ArgumentOutOfRangeException()
            };
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
            }
        }

        public class DataNodeTableAccessBridge
        {
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
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ActionRoutine))]
        public static void LuaCardAction(CardAction _Action, InGameCardBase _ReceivingCard, GameManager __instance,
            ref IEnumerator __result)
        {
            if (_Action.ActionName.LocalizationKey?.StartsWith("LuaCardAction") is true)
            {
                __result = __result.Prepend(LuaCardActionHelper(_Action, _ReceivingCard, __instance));
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.CardOnCardActionRoutine))]
        public static void LuaCardOnCardAction(CardOnCardAction _Action, InGameCardBase _ReceivingCard,
            InGameCardBase _GivenCard, GameManager __instance, ref IEnumerator __result)
        {
            if (_Action.ActionName.LocalizationKey?.StartsWith("LuaCardOnCardAction") is true)
            {
                __result = __result.Prepend(LuaCardOnCardActionHelper(_Action, _ReceivingCard, _GivenCard, __instance));
            }
        }

        public static readonly List<IEnumerator> Enumerators = new();

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
            lua["receive"] = new CardAccessBridge(_ReceivingCard);
            var luaScriptRetValues = new LuaScriptRetValues();
            lua["Ret"] = luaScriptRetValues;
            try
            {
                object? result;
                _ = lua.DoString(_Action.ActionName.ParentObjectID, _Action.ActionName.LocalizationKey);
                if (luaScriptRetValues.CheckKey(nameof(result), out result))
                {
                    waitTime = result switch
                    {
                        double d => Mathf.RoundToInt((float) d),
                        float f => Mathf.RoundToInt(f),
                        int i => i,
                        _ => waitTime
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            var buf = new List<IEnumerator>(Enumerators);
            Enumerators.Clear();
            var queue = new Queue<CoroutineController>();
            foreach (var enumerator in buf)
            {
                __instance.StartCoroutineEx(enumerator, out var controller);
                queue.Enqueue(controller);
            }

            if (waitTime > 0)
            {
                __instance.StartCoroutineEx(__instance.SpendDaytimePoints(waitTime, true, true, false, _ReceivingCard,
                    FadeToBlackTypes.None, "", false, false, null, null, null), out var controller);
                queue.Enqueue(controller);
            }

            while (queue.Count > 0)
            {
                var coroutineController = queue.Dequeue();
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
            lua["receive"] = new CardAccessBridge(_ReceivingCard);
            lua["given"] = new CardAccessBridge(_GivenCard);
            var luaScriptRetValues = new LuaScriptRetValues();
            lua["Ret"] = luaScriptRetValues;
            try
            {
                object? result;
                _ = lua.DoString(_Action.ActionName.ParentObjectID, _Action.ActionName.LocalizationKey);
                if (luaScriptRetValues.CheckKey(nameof(result), out result))
                {
                    waitTime = result switch
                    {
                        double d => Mathf.RoundToInt((float) d),
                        float f => Mathf.RoundToInt(f),
                        int i => i,
                        _ => waitTime
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            var buf = new List<IEnumerator>(Enumerators);
            Enumerators.Clear();
            var queue = new Queue<CoroutineController>();
            foreach (var enumerator in buf)
            {
                __instance.StartCoroutineEx(enumerator, out var controller);
                queue.Enqueue(controller);
            }

            if (waitTime > 0)
            {
                __instance.StartCoroutineEx(__instance.SpendDaytimePoints(waitTime, true, true, false, _ReceivingCard,
                    FadeToBlackTypes.None, "", false, false, null, null, null), out var controller);
                queue.Enqueue(controller);
            }

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


    public struct DataNode
    {
        public enum DataNodeType
        {
            Number,
            Str,
            Bool,
            Table,
            Nil
        }

        public DataNodeType NodeType;
        public double number;
        public string str;
        public bool _bool;
        public Dictionary<string, DataNode>? table;

        public static DataNode Nil
        {
            get
            {
                DataNode nil = default;
                nil.NodeType = DataNodeType.Nil;
                return nil;
            }
        }

        public DataNode(double number)
        {
            this.number = number;
            str = "";
            _bool = false;
            table = null;
            NodeType = DataNodeType.Number;
        }

        public DataNode(string str)
        {
            number = 0;
            this.str = str;
            _bool = false;
            table = null;
            NodeType = DataNodeType.Str;
        }

        public DataNode(bool b)
        {
            number = 0;
            str = "";
            _bool = b;
            table = null;
            NodeType = DataNodeType.Bool;
        }

        public DataNode(Dictionary<string, DataNode> dataNodes)
        {
            number = 0;
            str = "";
            _bool = false;
            table = dataNodes;
            NodeType = DataNodeType.Table;
        }

        public void Save(BinaryWriter binaryWriter)
        {
            binaryWriter.Write((int) NodeType);
            switch (NodeType)
            {
                case DataNodeType.Number:
                    binaryWriter.Write(number);
                    break;
                case DataNodeType.Str:
                    binaryWriter.Write(str);
                    break;
                case DataNodeType.Bool:
                    binaryWriter.Write(_bool);
                    break;
                case DataNodeType.Table:
                    if (table == null)
                    {
                        binaryWriter.Write(0);
                    }
                    else
                    {
                        binaryWriter.Write(table.Count);
                        foreach (var (key, node) in table)
                        {
                            binaryWriter.Write(key);
                            node.Save(binaryWriter);
                        }
                    }

                    break;
                case DataNodeType.Nil:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static DataNode Load(BinaryReader binaryReader)
        {
            DataNode node = default;
            node.NodeType = (DataNodeType) binaryReader.ReadInt32();
            switch (node.NodeType)
            {
                case DataNodeType.Number:
                    node.number = binaryReader.ReadDouble();
                    break;
                case DataNodeType.Str:
                    node.str = binaryReader.ReadString();
                    break;
                case DataNodeType.Bool:
                    node._bool = binaryReader.ReadBoolean();
                    break;
                case DataNodeType.Table:
                    var count = binaryReader.ReadInt32();
                    var dataNodes = new Dictionary<string, DataNode>();
                    for (var i = 0; i < count; i++)
                    {
                        var key = binaryReader.ReadString();
                        var dataNode = Load(binaryReader);
                        dataNodes[key] = dataNode;
                    }

                    node.table = dataNodes;
                    break;
                case DataNodeType.Nil:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return node;
        }
    }
}