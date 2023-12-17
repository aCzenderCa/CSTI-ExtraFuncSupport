﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.Helper;
using gfoidl.Base64;
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
    public object info
    {
        set => Debug.LogFormat("[Info] {0}", value is LuaTable table ? TableToString(table) : value);
    }

    public object debug
    {
        set => Debug.LogFormat("[Debug] {0}", value is LuaTable table ? TableToString(table) : value);
    }

    public object warn
    {
        set => Debug.LogWarningFormat("[Warn] {0}", value is LuaTable table ? TableToString(table) : value);
    }

    public object error
    {
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

public class CardAccessBridge(InGameCardBase? cardBase)
{
    public readonly InGameCardBase? CardBase = cardBase;

    public static readonly Regex KVDataCheck = new(@"zender\.luaSupportData\.\{(?<key>.+?)\}:\{(?<val>.+?)\}");

    public SimpleUniqueAccess? CardModel => CardBase != null ? new SimpleUniqueAccess(CardBase.CardModel) : null;

    public bool IsEquipped => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Equipment;
    public bool IsInHand => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Hand;
    public bool IsInBase => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Base;
    public bool IsInLocation => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Location;
    public bool IsInBackground => CardBase != null && CardBase.InBackground;

    public bool CheckInventory(bool useAll, params string[] uid)
    {
        return useAll ? uid.All(s => HasInInventory(s)) : uid.Any(s => HasInInventory(s));
    }

    public bool CheckTagInventory(bool useAll, params string[] tags)
    {
        return useAll ? tags.All(s => HasTagInInventory(s)) : tags.Any(s => HasTagInInventory(s));
    }

    public bool CheckRegexTagInventory(bool useAll, params string[] regexTags)
    {
        return useAll ? regexTags.All(s => HasRegexTagInInventory(s)) : regexTags.Any(s => HasRegexTagInInventory(s));
    }

    public bool HasInInventory(string uid, long needCount = 0)
    {
        if (CardBase == null) return false;
        if (!CardBase.IsInventoryCard) return false;
        if (UniqueIDScriptable.GetFromID<CardData>(uid) is not { } card) return false;
        if (needCount <= 0)
        {
            return CardBase.CardsInInventory.Any(slot => slot.CardModel.UniqueID == uid);
        }

        return CardBase.InventoryCount(card) >= needCount;
    }

    public bool HasTagInInventory(string tag, long needCount = 0)
    {
        if (CardBase == null) return false;
        if (!CardBase.IsInventoryCard) return false;
        if (needCount <= 0)
        {
            return CardBase.CardsInInventory.Any(slot => HasTag(slot.CardModel, tag));
        }

        var sum = CardBase.CardsInInventory.Where(slot => HasTag(slot.CardModel, tag)).Sum(slot => slot.CardAmt);
        return sum >= needCount;
    }

    public bool HasRegexTagInInventory(string regexTag, long needCount = 0)
    {
        if (CardBase == null) return false;
        if (!CardBase.IsInventoryCard) return false;
        if (needCount <= 0)
        {
            return CardBase.CardsInInventory.Any(slot => HasRegexTag(slot.CardModel, regexTag));
        }

        var sum = CardBase.CardsInInventory.Where(slot => HasRegexTag(slot.CardModel, regexTag))
            .Sum(slot => slot.CardAmt);
        return sum >= needCount;
    }

    public CardAccessBridge? LiquidInventory()
    {
        if (CardBase == null) return null;
        if (CardBase.LiquidEmpty) return null;
        return new CardAccessBridge(CardBase.ContainedLiquid);
    }

    public CardAccessBridge[]? this[long index]
    {
        get
        {
            if (CardBase == null) return null;
            if (!CardBase.IsInventoryCard) return null;
            if (index < 0 || index >= CardBase.CardsInInventory.Count) return null;
            return CardBase.CardsInInventory[Mathf.RoundToInt(index)].AllCards
                .Select(cardBase => new CardAccessBridge(cardBase))
                .ToArray();
        }
    }

    public int InventorySlotCount => CardBase && CardBase!.IsInventoryCard ? CardBase.CardsInInventory.Count : 0;

    public object? this[string key]
    {
        get
        {
            if (CardBase == null) return null;
            if (CardBase.DroppedCollections.TryGetValue(key, out var item))
            {
                return item;
            }

            var dictionary = CardBase.DroppedCollections.Keys.Select<string, KeyValuePair<string, string>?>(
                    s =>
                    {
                        var match = KVDataCheck.Match(s);
                        if (match.Success)
                        {
                            return new KeyValuePair<string, string>(match.Groups["key"].ToString(),
                                match.Groups["val"].ToString());
                        }

                        return null;
                    }).Where(pair => pair != null)
                .ToDictionary(pair => pair!.Value.Key, pair => pair!.Value.Value);
            if (!dictionary.TryGetValue(key, out var item1)) return null;
            if (double.TryParse(item1, out var result))
            {
                return result;
            }

            return item1;
        }

        set
        {
            if (CardBase == null) return;
            if (value is double i)
            {
                CardBase.DroppedCollections[key] = new Vector2Int(Mathf.RoundToInt((float) i), 0);
            }
            else
            {
                var strVal = value is LuaTable table
                    ? DebugBridge.TableToString(table)
                    : value;
                CardBase.DroppedCollections[
                    $"zender.luaSupportData.{{{key}}}:{{{strVal}}}"] = Vector2Int.one;
            }
        }
    }

    public string SlotType => CardBase != null ? CardBase.CurrentSlot.SlotType.ToString() : "nil";

    public string CardType => CardBase != null ? CardBase.CardModel.CardType.ToString() : "nil";

    public float Weight => CardBase != null ? CardBase.CurrentWeight : 0;

    public bool HasTag(string tag)
    {
        if (CardBase == null)
        {
            return false;
        }

        return HasTag(CardBase.CardModel, tag);
    }

    public static bool HasTag(CardData cardData, string tag)
    {
        if (cardData == null)
        {
            return false;
        }

        return cardData.CardTags.Any(cardTag =>
            cardTag.name == tag || cardTag.InGameName.DefaultText == tag);
    }

    public bool HasRegexTag(string regexTag)
    {
        if (CardBase == null)
        {
            return false;
        }

        return HasRegexTag(CardBase.CardModel, regexTag);
    }

    public static bool HasRegexTag(CardData cardData, string regexTag)
    {
        if (cardData == null)
        {
            return false;
        }

        var tag = new Regex(regexTag);

        return cardData.CardTags.Any(cardTag =>
            tag.IsMatch(cardTag.name) || tag.IsMatch(cardTag.InGameName.DefaultText));
    }

    public int TravelCardIndex => CardBase != null ? CardBase.TravelCardIndex : -1;

    public string Id => CardBase != null ? CardBase.CardModel.UniqueID : "";

    private DataNode? _dataNode;
    private static readonly Regex DataNodeReg = new(@"^LNbt\|\>(?<nbt>.+?)\<\|$");

    // language=Lua
    [TestCode("""
              receive:InitData()
              local d = receive.Data
              if d["i"] == nil then
                d["i"] = 10
              else
                d["i"] = d["i"] + 1
              end
              receive:SaveData()
              """)]
    public DataNodeTableAccessBridge? Data
    {
        get
        {
            if (CardBase == null) return null;
            if (_dataNode is {NodeType: DataNode.DataNodeType.Table}) goto end;
            foreach (var key in CardBase.DroppedCollections.Keys)
            {
                if (DataNodeReg.Match(key) is not { } match) continue;
                using var memoryStream =
                    new MemoryStream(Base64.Default.Decode(match.Groups["nbt"].Value.AsSpan()));
                var binaryReader = new BinaryReader(memoryStream);
                _dataNode = DataNode.Load(binaryReader);
                break;
            }

            end: ;
            return _dataNode is {NodeType: DataNode.DataNodeType.Table}
                ? new DataNodeTableAccessBridge(_dataNode.Value.table)
                : null;
        }
    }

    public void InitData(DataNodeTableAccessBridge? initData = null)
    {
        if (Data == null)
        {
            _dataNode = initData?.Table == null
                ? new DataNode(new Dictionary<string, DataNode>())
                : new DataNode(initData.Table);
        }
    }

    public void SaveData()
    {
        if (CardBase == null) return;
        if (_dataNode is not {NodeType: DataNode.DataNodeType.Table} dataNode) return;
        var memoryStream = new MemoryStream();
        var binaryWriter = new BinaryWriter(memoryStream);
        dataNode.Save(binaryWriter);
        var array = memoryStream.ToArray();
        memoryStream.Close();
        if (CardBase.DroppedCollections.FirstOrDefault(pair => DataNodeReg.IsMatch(pair.Key)) is {Key: not null} p)
        {
            CardBase.DroppedCollections.Remove(p.Key);
        }

        CardBase.DroppedCollections[$"LNbt|>{Base64.Default.Encode(array)}<|"] = Vector2Int.zero;
    }

    public float Spoilage
    {
        get => CardBase != null ? CardBase.CurrentSpoilage : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Spoilage));
    }

    public float Usage
    {
        get => CardBase != null ? CardBase.CurrentUsageDurability : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Usage));
    }

    public float Fuel
    {
        get => CardBase != null ? CardBase.CurrentFuel : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Fuel));
    }

    public float Progress
    {
        get => CardBase != null ? CardBase.CurrentProgress : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Progress));
    }

    public float Special1
    {
        get => CardBase != null ? CardBase.CurrentSpecial1 : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special1));
    }

    public float Special2
    {
        get => CardBase != null ? CardBase.CurrentSpecial2 : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special2));
    }

    public float Special3
    {
        get => CardBase != null ? CardBase.CurrentSpecial3 : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special3));
    }

    public float Special4
    {
        get => CardBase != null ? CardBase.CurrentSpecial4 : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special4));
    }

    public float LiquidQuantity
    {
        get => CardBase != null && CardBase.IsLiquid ? CardBase.CurrentLiquidQuantity : 0;
        set => Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Liquid));
    }

    private IEnumerator ModifyDurability(float val, DurabilitiesTypes types)
    {
        if (CardBase == null) yield break;
        var cardBaseCardModel = CardBase.CardModel;
        IEnumerator? enumerator;
        switch (types)
        {
            case DurabilitiesTypes.Spoilage:
                if (inner_modify(cardBaseCardModel.SpoilageTime, CardBase.CurrentSpoilage, CardBase,
                        ref CardBase.CurrentSpoilage, ref CardBase.SpoilEmpty, ref CardBase.SpoilFull,
                        out enumerator))
                    yield return enumerator;
                break;
            case DurabilitiesTypes.Usage:
                if (inner_modify(cardBaseCardModel.UsageDurability, CardBase.CurrentUsageDurability, CardBase,
                        ref CardBase.CurrentUsageDurability, ref CardBase.UsageEmpty, ref CardBase.UsageFull,
                        out enumerator))
                    yield return enumerator;
                break;
            case DurabilitiesTypes.Fuel:
                if (inner_modify(cardBaseCardModel.FuelCapacity, CardBase.CurrentFuel, CardBase,
                        ref CardBase.CurrentFuel, ref CardBase.FuelEmpty, ref CardBase.FuelFull,
                        out enumerator))
                    yield return enumerator;
                break;
            case DurabilitiesTypes.Progress:
                if (inner_modify(cardBaseCardModel.Progress, CardBase.CurrentProgress, CardBase,
                        ref CardBase.CurrentProgress, ref CardBase.ProgressEmpty, ref CardBase.ProgressFull,
                        out enumerator))
                    yield return enumerator;
                break;
            case DurabilitiesTypes.Liquid:
                if (!CardBase.IsLiquid)
                {
                    yield break;
                }

                var rawLiquidEmpty = CardBase.LiquidEmpty;
                CardBase.CurrentLiquidQuantity += val;
                CardBase.CurrentLiquidQuantity = CardBase.CurrentMaxLiquidQuantity > 0f
                    ? Mathf.Clamp(CardBase.CurrentLiquidQuantity, 0f, CardBase.CurrentMaxLiquidQuantity)
                    : Mathf.Min(CardBase.CurrentLiquidQuantity, 0f);
                CardBase.WeightHasChanged();
                if (!rawLiquidEmpty && CardBase.LiquidEmpty)
                {
                    yield return GameManager.PerformActionAsEnumerator(CardData.OnEvaporatedAction, CardBase, false);
                }

                break;
            case DurabilitiesTypes.Special1:
                if (inner_modify(cardBaseCardModel.SpecialDurability1, CardBase.CurrentSpecial1, CardBase,
                        ref CardBase.CurrentSpecial1, ref CardBase.Special1Empty, ref CardBase.Special1Full,
                        out enumerator))
                    yield return enumerator;
                break;
            case DurabilitiesTypes.Special2:
                if (inner_modify(cardBaseCardModel.SpecialDurability2, CardBase.CurrentSpecial2, CardBase,
                        ref CardBase.CurrentSpecial2, ref CardBase.Special2Empty, ref CardBase.Special2Full,
                        out enumerator))
                    yield return enumerator;
                break;
            case DurabilitiesTypes.Special3:
                if (inner_modify(cardBaseCardModel.SpecialDurability3, CardBase.CurrentSpecial3, CardBase,
                        ref CardBase.CurrentSpecial3, ref CardBase.Special3Empty, ref CardBase.Special3Full,
                        out enumerator))
                    yield return enumerator;
                break;
            case DurabilitiesTypes.Special4:
                if (inner_modify(cardBaseCardModel.SpecialDurability4, CardBase.CurrentSpecial4, CardBase,
                        ref CardBase.CurrentSpecial4, ref CardBase.Special4Empty, ref CardBase.Special4Full,
                        out enumerator))
                    yield return enumerator;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(types), types, null);
        }

        CardBase.CardVisuals.RefreshDurabilities();
        yield break;

        bool inner_modify(DurabilityStat durabilityStat, float rawCurrentSpoilage, InGameCardBase card,
            ref float durabilityStat_ref, ref bool durabilityStat_ref_empty, ref bool durabilityStat_ref_full,
            out IEnumerator? _enumerator)
        {
            _enumerator = null;
            if (!durabilityStat)
            {
                return false;
            }

            if (val >= durabilityStat.Max)
            {
                durabilityStat_ref_empty = false;
                durabilityStat_ref = durabilityStat.Max;
                if (rawCurrentSpoilage < durabilityStat.Max)
                {
                    durabilityStat_ref_full = true;
                    _enumerator = card.PerformDurabilitiesActions(true);
                    return true;
                }

                return false;
            }

            if (val <= 0)
            {
                durabilityStat_ref_full = false;
                durabilityStat_ref = 0;
                if (rawCurrentSpoilage > 0)
                {
                    durabilityStat_ref_empty = true;
                    _enumerator = card.PerformDurabilitiesActions(true);
                    return true;
                }

                return false;
            }

            durabilityStat_ref = val;
            durabilityStat_ref_empty = false;
            durabilityStat_ref_full = false;
            return false;
        }
    }

    public void AddCard(string id, int count = 1, LuaTable? ext = null)
    {
        var cardData = UniqueIDScriptable.GetFromID<CardData>(id);
        if (cardData == null)
        {
            return;
        }

        var tDur = new TransferedDurabilities
        {
            Liquid = cardData.CardType == CardTypes.Liquid ? count : 0,
            Usage = cardData.UsageDurability.Copy(),
            Fuel = cardData.FuelCapacity.Copy(),
            Spoilage = cardData.SpoilageTime.Copy(),
            ConsumableCharges = cardData.Progress.Copy(),
            Special1 = cardData.SpecialDurability1.Copy(),
            Special2 = cardData.SpecialDurability2.Copy(),
            Special3 = cardData.SpecialDurability3.Copy(),
            Special4 = cardData.SpecialDurability4.Copy(),
        };
        var sLiq = new SpawningLiquid
        {
            LiquidCard = cardData.DefaultLiquidContained.LiquidCard,
            StayEmpty = !cardData.DefaultLiquidContained.LiquidCard
        };
        DataNodeTableAccessBridge? initData = null;
        if (ext != null)
        {
            tDur.Usage.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Usage)]);
            tDur.Fuel.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Fuel)]);
            tDur.Spoilage.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Spoilage)]);
            tDur.ConsumableCharges.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.ConsumableCharges)]);
            tDur.Liquid.TryModBy(ext[nameof(TransferedDurabilities.Liquid)]);
            tDur.Special1.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special1)]);
            tDur.Special2.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special2)]);
            tDur.Special3.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special3)]);
            tDur.Special4.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special4)]);

            var card =
                (ext[nameof(SpawningLiquid.LiquidCard)] as SimpleUniqueAccess)?.UniqueIDScriptable as CardData;
            sLiq.LiquidCard = card;
            sLiq.StayEmpty = !card;

            count.TryModBy(ext[nameof(count)]);

            if (ext[nameof(initData)] is DataNodeTableAccessBridge dataNodeTable)
                initData = dataNodeTable;
        }

        var i = 0;
        do
        {
            i += 1;

            Enumerators.Add(GameManager.Instance.MoniAddCard(cardData, CardBase, tDur, true,
                sLiq, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1), SimpleUniqueAccess.SetInitData,
                initData));
        } while (i < count && cardData.CardType != CardTypes.Liquid);
    }

    public void Remove(bool doDrop)
    {
        Enumerators.Add(GameManager.Instance.RemoveCard(CardBase, true, doDrop));
    }
}

public class GameStatAccessBridge
{
    public readonly InGameStat GameStat;

    public GameStatAccessBridge(InGameStat gameStat)
    {
        GameStat = gameStat;
    }

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