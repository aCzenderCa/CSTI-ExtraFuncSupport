using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class DataAccessTool
{
    // language=Lua
    [LuaFunc, TestCode("""debug.debug=GetCard("8695a7aa22521aa45be582d3c1558f78")""")]
    public static CardData GetCard(string id)
    {
        return UniqueIDScriptable.GetFromID<CardData>(id);
    }

    [LuaFunc]
    public static CardAccessBridge GetGameCard(string id)
    {
        var inGameCardBases = new List<InGameCardBase>();
        GameManager.Instance.CardIsOnBoard(UniqueIDScriptable.GetFromID<CardData>(id), true,
            _Results: inGameCardBases);
        return new CardAccessBridge(inGameCardBases.FirstOrDefault());
    }

    [LuaFunc]
    public static CardAccessBridge? GetGameCardByTag(string tag)
    {
        var inGameCardBases = new List<InGameCardBase>(GameManager.Instance.AllCards.Where(cardBase =>
            cardBase.CardModel.CardTags.Any(cardTag =>
                cardTag != null && (cardTag.InGameName.DefaultText == tag || cardTag.name == tag))));
        return inGameCardBases.FirstOrDefault() is { } card ? new CardAccessBridge(card) : null;
    }

    public static IEnumerable<InGameCardBase> ProcessType(this IEnumerable<InGameCardBase> enumerable, string type,
        CardData? cardData)
    {
        List<InGameCardBase> list = [];
        return type switch
        {
            nameof(SlotsTypes.Equipment) => enumerable.Where(cardBase =>
                cardBase.CurrentSlotInfo.SlotType == SlotsTypes.Equipment),
            nameof(SlotsTypes.Hand) when cardData => GameManager.Instance.CardIsInHand(cardData, _Results: list)
                ? list
                : list,
            nameof(SlotsTypes.Hand) => enumerable.Where(
                cardBase => cardBase.CurrentSlotInfo.SlotType == SlotsTypes.Hand),
            nameof(SlotsTypes.Base) => enumerable.Where(cardBase =>
                cardBase.CurrentSlotInfo.SlotType == SlotsTypes.Base),
            nameof(SlotsTypes.Location) => enumerable.Where(cardBase =>
                cardBase.CurrentSlotInfo.SlotType == SlotsTypes.Location),
            nameof(SlotsTypes.Inventory) => enumerable.Where(cardBase =>
                cardBase.CurrentSlotInfo.SlotType == SlotsTypes.Inventory),
            nameof(SlotsTypes.Environment) => [GameManager.Instance.CurrentEnvironmentCard],
            nameof(SlotsTypes.Weather) => [GameManager.Instance.CurrentWeatherCard],
            nameof(SlotsTypes.Explorable) => [GameManager.Instance.CurrentExplorableCard],
            "OnlyBackGround" => enumerable.Where(cardBase => cardBase.InBackground),
            "NotBackGround" => enumerable.Where(cardBase => !cardBase.InBackground),
            _ => enumerable
        };
    }

    public static List<CardAccessBridge> IntoBridge(this IEnumerable<InGameCardBase> enumerable)
    {
        return enumerable.Select(card => new CardAccessBridge(card)).ToList();
    }

    // language=Lua
    [LuaFunc, TestCode("""
                       local uid = "8695a7aa22521aa45be582d3c1558f78"
                       local ext = { type = "Base" }
                       debug.debug = GetGameCards(uid,ext)[0].CardBase
                       """)]
    public static List<CardAccessBridge>? GetGameCards(string id, LuaTable? ext = null)
    {
        try
        {
            var list = new List<InGameCardBase>();
            var cardData = UniqueIDScriptable.GetFromID<CardData>(id);
            if (cardData == null)
            {
                return null;
            }

            GameManager.Instance.CardIsOnBoard(cardData, true, _Results: list);

            var o = ext?["type"];
            switch (o)
            {
                case string s:
                    return list.ProcessType(s, cardData).IntoBridge();
                case LuaTable table:
                {
                    IEnumerable<InGameCardBase> enu = list;
                    foreach (var tableKey in table.Keys)
                    {
                        if (table[tableKey] is string t_s)
                        {
                            enu = enu.ProcessType(t_s, cardData);
                        }
                    }

                    return enu.IntoBridge();
                }
                default:
                    return list.IntoBridge();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            return null;
        }
    }

    [LuaFunc]
    public static List<CardAccessBridge>? GetGameCardsByTag(string tag, LuaTable? ext = null)
    {
        try
        {
            var inGameCardBases = new List<InGameCardBase>(GameManager.Instance.AllCards.Where(cardBase =>
                cardBase.CardModel.CardTags.Any(cardTag =>
                    cardTag != null && (cardTag.InGameName.DefaultText == tag || cardTag.name == tag))));
            var o = ext?["type"];
            switch (o)
            {
                case string s:
                    return inGameCardBases.ProcessType(s, null).IntoBridge();
                case LuaTable table:
                {
                    IEnumerable<InGameCardBase> enu = inGameCardBases;
                    foreach (var tableKey in table.Keys)
                    {
                        if (table[tableKey] is string t_s)
                        {
                            enu = enu.ProcessType(t_s, null);
                        }
                    }

                    return enu.IntoBridge();
                }
                default:
                    return inGameCardBases.IntoBridge();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            return null;
        }
    }

    [LuaFunc]
    public static int CountCardOnBoard(string id, bool _CountInInventories = true, bool _CountInBackground = false)
    {
        var list = new List<InGameCardBase>();
        GameManager.Instance.CardIsOnBoard(UniqueIDScriptable.GetFromID<CardData>(id), true,
            _CountInInventories, false, _CountInBackground, list);
        return list.Count;
    }

    [LuaFunc]
    public static int CountCardInBase(string id, bool _CountInInventories = true)
    {
        var list = new List<InGameCardBase>();
        GameManager.Instance.CardIsOnBoard(UniqueIDScriptable.GetFromID<CardData>(id), _ActiveImprovements: false,
            _CountInInventories: _CountInInventories, _Results: list);
        return list.Count(cardBase => cardBase.CurrentSlotInfo.SlotType == SlotsTypes.Base);
    }

    [LuaFunc]
    public static int CountCardEquipped(string id)
    {
        var list = new List<InGameCardBase>();
        GameManager.Instance.CardIsOnBoard(UniqueIDScriptable.GetFromID<CardData>(id), false, false, _Results: list);
        return list.Count(cardBase => cardBase.CurrentSlotInfo.SlotType == SlotsTypes.Equipment);
    }

    [LuaFunc]
    public static int CountCardInHand(string id, bool _CountInInventories = true)
    {
        var list = new List<InGameCardBase>();
        GameManager.Instance.CardIsInHand(UniqueIDScriptable.GetFromID<CardData>(id), _CountInInventories, list);
        return list.Count;
    }

    [LuaFunc]
    public static int CountCardInLocation(string id, bool _CountInInventories = true)
    {
        var list = new List<InGameCardBase>();
        GameManager.Instance.CardIsInLocation(UniqueIDScriptable.GetFromID<CardData>(id), _CountInInventories,
            false, list);
        return list.Count;
    }

    [LuaFunc]
    public static GameStat GetStat(string id)
    {
        return UniqueIDScriptable.GetFromID<GameStat>(id);
    }

    [LuaFunc]
    public static GameStatAccessBridge GetGameStat(string id)
    {
        return new GameStatAccessBridge(GameManager.Instance.StatsDict[GetStat(id)]);
    }
}

public class DebugBridge
{
    public object? info
    {
        get => null;
        set => Debug.LogFormat("[Info] {0}", value is LuaTable table ? TableToString(table) : value);
    }

    public object? debug
    {
        get => null;
        set => Debug.LogFormat("[Debug] {0}", value is LuaTable table ? TableToString(table) : value);
    }

    public object? warn
    {
        get => null;
        set => Debug.LogWarningFormat("[Warn] {0}", value is LuaTable table ? TableToString(table) : value);
    }

    public object? error
    {
        get => null;
        set => Debug.LogErrorFormat("[Error] {0}", value is LuaTable table ? TableToString(table) : value);
    }

    public static string TableToString(LuaTable table, List<LuaTable>? cache = null)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("{");
        foreach (var (key, val) in table.Items())
        {
            if (stringBuilder.Length != 1)
            {
                stringBuilder.Append(", ");
            }

            if (key is LuaTable keyTable && cache?.Contains(keyTable) is not true)
            {
                stringBuilder.Append(TableToString(keyTable,
                    cache == null ? [table] : cache.Append(table).ToList()));
            }
            else
            {
                stringBuilder.Append(key);
            }

            stringBuilder.Append(" : ");
            if (val is LuaTable valTable && cache?.Contains(valTable) is not true)
            {
                stringBuilder.Append(TableToString(valTable,
                    cache == null ? [table] : cache.Append(table).ToList()));
            }
            else
            {
                stringBuilder.Append(val);
            }
        }

        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }
}

public class GameStatAccessBridge(InGameStat gameStat)
{
    public readonly InGameStat GameStat = gameStat;

    public float Value
    {
        get => GameStat.SimpleCurrentValue;
        set => GameManager.Instance.ChangeStatValueTo(GameStat, value);
    }

    public float Rate
    {
        get => GameStat.SimpleRatePerTick;
        set => GameManager.Instance.ChangeStatRateTo(GameStat, value);
    }
}

public static class CLR2Lua
{
    public static LuaTable ToLuaTable<TK, TV>(this Dictionary<TK, TV> dictionary)
    {
        var luaTable = LuaRuntime.TempTable();
        foreach (var (key, value) in dictionary)
        {
            luaTable[key] = value;
        }

        return luaTable;
    }

    public static LuaTable ToLuaTable<TItem>(this List<TItem> list)
    {
        var luaTable = LuaRuntime.TempTable();
        for (var i = 0; i < list.Count; i++)
        {
            luaTable[i + 1] = list[i];
        }

        return luaTable;
    }
}