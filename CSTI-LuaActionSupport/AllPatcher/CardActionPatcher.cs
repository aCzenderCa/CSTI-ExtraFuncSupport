using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using static CSTI_LuaActionSupport.AllPatcher.LuaRegister;
using HarmonyLib;
using ILMerge;
using NLua;
using UnityEngine;
using Lua = NLua.Lua;

namespace CSTI_LuaActionSupport.AllPatcher;

public static class CardActionPatcher
{
    public static readonly Lua LuaRuntime = new();
    public static readonly DebugBridge DebugBridge = new();
    public static readonly LuaTable ModData;
    public static readonly Dictionary<string, DataNode> GSaveData = new();
    public static readonly Dictionary<int, Dictionary<string, DataNode>> GSlotSaveData = new();
    private const string _InnerFuncBase = "CSTI_LuaActionSupport__InnerFunc";
    public static LuaTable InnerFuncBase => (LuaTable) LuaRuntime[_InnerFuncBase];


    static CardActionPatcher()
    {
        LuaRuntime.State.OpenLibs();
        LuaRuntime.State.Encoding = Encoding.UTF8;
        LuaRuntime["debug"] = DebugBridge;
        LuaRuntime[nameof(SimpleAccessTool)] = new SimpleAccessTool();

        LuaRuntime.Register(typeof(DataAccessTool));
        LuaRuntime.Register(typeof(CardActionPatcher));
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

    [LuaFunc, TestCode("""
                       SaveCurrentSlot("__test",10)
                       """)]
    public static void SaveCurrentSlot(string key, object val)
    {
        CurrentGSlotSaveData().CommonSave(key, val);
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

    private static object? CommonLoad(this ref DataNode node)
    {
        return node.NodeType switch
        {
            DataNode.DataNodeType.Number => node.number,
            DataNode.DataNodeType.Str => node.str,
            DataNode.DataNodeType.Bool => node._bool,
            DataNode.DataNodeType.Table => new DataNodeTableAccessBridge(node.table),
            DataNode.DataNodeType.Nil => null,
            DataNode.DataNodeType.Vector2 => node.vector2,
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
        public LuaTable? LuaKeys
        {
            get
            {
                if (Keys == null) return null;
                LuaRuntime.NewTable("__temp");
                var luaTable = LuaRuntime.GetTable("__temp");
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
        public Dictionary<string, DataNode>.KeyCollection? Keys => Table?.Keys;
        public int Count => Table?.Count ?? 0;

        public DataNodeTableAccessBridge(Dictionary<string, DataNode>? table)
        {
            Table = table;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardAction), nameof(CardAction.WillHaveAnEffect))]
    public static void LuaActionWillHaveAnEffect(CardAction __instance, ref bool __result)
    {
        if (__instance.ActionName.LocalizationKey?.StartsWith("LuaCardAction") is true)
        {
            __result = true;
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
        var miniWaitTime = 0;
        lua["receive"] = new CardAccessBridge(_ReceivingCard);
        var luaScriptRetValues = new LuaScriptRetValues();
        lua["Ret"] = luaScriptRetValues;
        try
        {
            object? result;
            object? miniTime;
            _ = lua.DoString(_Action.ActionName.ParentObjectID, _Action.ActionName.LocalizationKey);
            if (luaScriptRetValues.CheckKey(nameof(result), out result))
            {
                waitTime.TryModBy(result);
            }

            if (luaScriptRetValues.CheckKey(nameof(miniTime), out miniTime))
            {
                miniWaitTime.TryModBy(miniTime);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        var queue = __instance.ProcessCache().ProcessTime(_ReceivingCard, miniWaitTime, waitTime);

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
        var miniWaitTime = 0;
        lua["receive"] = new CardAccessBridge(_ReceivingCard);
        lua["given"] = new CardAccessBridge(_GivenCard);
        var luaScriptRetValues = new LuaScriptRetValues();
        lua["Ret"] = luaScriptRetValues;
        try
        {
            object? result;
            object? miniTime;
            _ = lua.DoString(_Action.ActionName.ParentObjectID, _Action.ActionName.LocalizationKey);
            if (luaScriptRetValues.CheckKey(nameof(result), out result))
            {
                waitTime.TryModBy(result);
            }

            if (luaScriptRetValues.CheckKey(nameof(miniTime), out miniTime))
            {
                miniWaitTime.TryModBy(miniTime);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        var queue = __instance.ProcessCache().ProcessTime(_ReceivingCard, miniWaitTime, waitTime);

        while (queue.Count > 0)
        {
            var coroutineController = queue.Dequeue();
            while (coroutineController.state == CoroutineState.Running)
            {
                yield return null;
            }
        }
    }

    private static Queue<CoroutineController> ProcessTime(this Queue<CoroutineController> queue,
        InGameCardBase _ReceivingCard,
        int miniWaitTime, int waitTime)
    {
        var __instance = GameManager.Instance;
        if (miniWaitTime > 0)
        {
            GameManager.Instance.CurrentMiniTicks += miniWaitTime;
            waitTime += GameManager.Instance.CurrentMiniTicks / 5;
            GameManager.Instance.CurrentMiniTicks %= 5;
            GraphicsManager.Instance.UpdateTimeInfo(false);
        }

        if (waitTime > 0)
        {
            queue.Enqueue(__instance.SpendDaytimePoints(waitTime, _ReceivingCard).Start(__instance));
        }

        return queue;
    }
}

public struct DataNode
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DataNodeDataUnion
    {
        [FieldOffset(8)] public double number;
        [FieldOffset(8)] public bool _bool;
        [FieldOffset(8)] public Vector2 vector2;

        [FieldOffset(0)] public string? str;
        [FieldOffset(0)] public Dictionary<string, DataNode>? table;

        public DataNodeDataUnion(double num)
        {
            number = num;
        }

        public DataNodeDataUnion(string str)
        {
            this.str = str;
        }

        public DataNodeDataUnion(bool b)
        {
            _bool = b;
        }

        public DataNodeDataUnion(Vector2 vector2)
        {
            this.vector2 = vector2;
        }

        public DataNodeDataUnion(Dictionary<string, DataNode> table)
        {
            this.table = table;
        }
    }

    public enum DataNodeType
    {
        Number,
        Str,
        Bool,
        Table,
        Nil,
        Vector2
    }

    public DataNodeType NodeType;
    public DataNodeDataUnion NodeData;

    public double number => NodeData.number;
    public string? str => NodeData.str;
    public bool _bool => NodeData._bool;
    public Vector2 vector2 => NodeData.vector2;
    public Dictionary<string, DataNode>? table => NodeData.table;

    public static DataNode EmptyTable => new(new Dictionary<string, DataNode>());

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
        NodeType = DataNodeType.Number;
        NodeData = new DataNodeDataUnion(number);
    }

    public DataNode(string str)
    {
        NodeType = DataNodeType.Str;
        NodeData = new DataNodeDataUnion(str);
    }

    public DataNode(bool b)
    {
        NodeType = DataNodeType.Bool;
        NodeData = new DataNodeDataUnion(b);
    }

    public DataNode(Vector2 vector2)
    {
        NodeType = DataNodeType.Vector2;
        NodeData = new DataNodeDataUnion(vector2);
    }

    public DataNode(Dictionary<string, DataNode> dataNodes)
    {
        NodeType = DataNodeType.Table;
        NodeData = new DataNodeDataUnion(dataNodes);
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
                binaryWriter.Write(str ?? "");
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
                    binaryWriter.Write(table.Count(pair => pair.Value.NodeType != DataNodeType.Nil));
                    foreach (var (key, node) in table)
                    {
                        if (node.NodeType != DataNodeType.Nil)
                        {
                            binaryWriter.Write(key);
                            node.Save(binaryWriter);
                        }
                    }
                }

                break;
            case DataNodeType.Nil:
                break;
            case DataNodeType.Vector2:
                binaryWriter.Write(vector2.x);
                binaryWriter.Write(vector2.y);
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
                node.NodeData = new DataNodeDataUnion(binaryReader.ReadDouble());
                break;
            case DataNodeType.Str:
                node.NodeData = new DataNodeDataUnion(binaryReader.ReadString());
                break;
            case DataNodeType.Bool:
                node.NodeData = new DataNodeDataUnion(binaryReader.ReadBoolean());
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

                node.NodeData = new DataNodeDataUnion(dataNodes);
                break;
            case DataNodeType.Nil:
                break;
            case DataNodeType.Vector2:
                var x = binaryReader.ReadSingle();
                var y = binaryReader.ReadSingle();
                node.NodeData = new DataNodeDataUnion(new Vector2(x, y));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return node;
    }
}